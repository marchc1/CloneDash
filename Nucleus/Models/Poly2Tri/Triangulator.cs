using System.Diagnostics;

namespace Poly2Tri;

/// <summary>
/// Constrained Delaunay triangulator, based on Poly2Tri's sweep
/// <br/><b>NOTE:</b> This is NOT thread safe, use <see cref="Shared"/> per thread.
/// </summary>
public sealed class Triangulator
{
	[ThreadStatic] static Triangulator? shared;
	public static Triangulator Shared => shared ??= new();

	private const float K_ALPHA = 0.3f;
	private const double PI_3div4 = 3 * Math.PI / 4;
	private const double PI_div2 = 1.57079632679489661923;
	private const double EPSILON = 1e-12;

	private const int CW = 0, CCW = 1, COLLINEAR = 2;
	private const byte CONSTRAINED = 1, BOUNDARY = 2;

	// Input
	double[] px = new double[64], py = new double[64];
	int pointCount;
	int[] loopStart = new int[4], loopLength = new int[4];
	int loopCount;
	int[] userEdges = new int[64];
	int userEdgeCount;

	// Sweep points
	int[] order = new int[64];
	int orderCount;
	byte[] inLoop = new byte[64];
	int head, tail;

	// Edges, listed on their upper point
	int[] edgeP = new int[64], edgeQ = new int[64], edgeNext = new int[64];
	byte[] edgeKind = new byte[64];
	int edgeCount;
	int[] firstEdge = new int[64], lastEdge = new int[64];

	// Triangles, three slots each
	int[] tp = new int[192], tn = new int[192];
	byte[] tc = new byte[192];
	bool[] td = new bool[192];
	bool[] interior = new bool[64];
	int triCount;
	int[] result = new int[64];
	int resultCount;

	// Advancing front
	int[] nodePoint = new int[64], nodeTri = new int[64], nodeNext = new int[64], nodePrev = new int[64];
	int nodeCount;
	int frontHead, frontSearch;

	// Current edge event
	int eeP, eeQ;
	byte eeKind;
	bool eeRight;

	int basinLeft, basinBottom, basinRight;
	double basinWidth;
	bool basinLeftHighest;

	int[] stack = new int[64];

	public int PointCount => pointCount;

	public void Clear() {
		pointCount = 0;
		loopCount = 0;
		userEdgeCount = 0;
	}

	public int AddPoint(double x, double y) {
		if (pointCount + 2 >= px.Length) {
			Array.Resize(ref px, px.Length * 2);
			Array.Resize(ref py, py.Length * 2);
		}
		px[pointCount] = x;
		py[pointCount] = y;
		return pointCount++;
	}

	/// <summary>
	/// Closes points [start, start + count) into a loop, the first loop is the outline and the rest are holes.
	/// Points that aren't in a loop are steiner points.
	/// </summary>
	public void AddLoop(int start, int count) {
		if (loopCount == loopStart.Length) {
			Array.Resize(ref loopStart, loopCount * 2);
			Array.Resize(ref loopLength, loopCount * 2);
		}
		loopStart[loopCount] = start;
		loopLength[loopCount] = count;
		loopCount++;
	}

	public void AddEdge(int a, int b) {
		if (userEdgeCount * 2 + 2 > userEdges.Length)
			Array.Resize(ref userEdges, userEdges.Length * 2);
		userEdges[userEdgeCount * 2] = a;
		userEdges[userEdgeCount * 2 + 1] = b;
		userEdgeCount++;
	}

	/// <summary>
	/// Triangulates and appends three point indices per triangle to <paramref name="output"/>.
	/// </summary>
	public void Triangulate(List<int> output) {
		if (loopCount == 0 || loopLength[0] == 0) return;

		triCount = 0;
		nodeCount = 0;
		edgeCount = 0;
		orderCount = 0;
		resultCount = 0;

		int slots = pointCount + 2;
		grow(ref firstEdge, slots);
		grow(ref lastEdge, slots);
		grow(ref inLoop, slots);
		grow(ref order, slots);
		Array.Fill(firstEdge, -1, 0, slots);
		Array.Fill(lastEdge, -1, 0, slots);
		Array.Clear(inLoop, 0, slots);

		// Constrained edges go in twice, before and after the loops. Dropping either changes some results.
		for (int i = 0; i < userEdgeCount; i++)
			addEdge(userEdges[i * 2], userEdges[i * 2 + 1], CONSTRAINED);

		for (int l = 0; l < loopCount; l++) {
			int first = orderCount;
			for (int i = 0; i < loopLength[l]; i++) {
				int p = loopStart[l] + i;
				order[orderCount++] = p;
				inLoop[p] = 1;
			}
			int last = orderCount - 1;
			if (first > last) throw new ArgumentOutOfRangeException(nameof(loopLength), "Empty loop");

			if (same(order[first], order[last]))
				last = --orderCount - 1;

			for (int i = first; i <= last; i++) {
				int j = i < last ? i + 1 : first;
				addEdge(order[i], order[j], CONSTRAINED | BOUNDARY);
			}
		}

		for (int p = 0; p < pointCount; p++)
			if (inLoop[p] == 0)
				order[orderCount++] = p;

		for (int i = 0; i < userEdgeCount; i++)
			addEdge(userEdges[i * 2], userEdges[i * 2 + 1], CONSTRAINED);

		initTriangulation();
		createAdvancingFront();
		sweepPoints();
		finalizationPolygon();

		for (int i = 0; i < resultCount; i++) {
			int t = result[i] * 3;
			int a = tp[t], b = tp[t + 1], c = tp[t + 2];
			if (a >= pointCount || b >= pointCount || c >= pointCount) continue;
			output.Add(a);
			output.Add(b);
			output.Add(c);
		}
	}

	static void grow<T>(ref T[] array, int size) {
		if (array.Length < size)
			Array.Resize(ref array, Math.Max(size, array.Length * 2));
	}

	bool same(int a, int b) => px[a] == px[b] && py[a] == py[b];

	void addEdge(int p1, int p2, byte kind) {
		int p = p1, q = p2;
		if (py[p1] > py[p2]) {
			q = p1;
			p = p2;
		}
		else if (py[p1] == py[p2]) {
			if (px[p1] > px[p2]) {
				q = p1;
				p = p2;
			}
			else if (px[p1] == px[p2])
				throw new Exception("Edge::Edge: p1 == p2");
		}

		if (edgeCount == edgeP.Length) {
			Array.Resize(ref edgeP, edgeCount * 2);
			Array.Resize(ref edgeQ, edgeCount * 2);
			Array.Resize(ref edgeNext, edgeCount * 2);
			Array.Resize(ref edgeKind, edgeCount * 2);
		}

		int e = edgeCount++;
		edgeP[e] = p;
		edgeQ[e] = q;
		edgeKind[e] = kind;
		edgeNext[e] = -1;
		if (lastEdge[q] == -1) firstEdge[q] = e;
		else edgeNext[lastEdge[q]] = e;
		lastEdge[q] = e;
	}

	void initTriangulation() {
		if (orderCount == 0) throw new ArgumentOutOfRangeException(nameof(orderCount), "No points");

		double xmax = px[order[0]];
		double xmin = px[order[0]];
		double ymax = py[order[0]];
		double ymin = py[order[0]];

		for (int i = 0; i < orderCount; i++) {
			int p = order[i];
			if (px[p] > xmax) xmax = px[p];
			if (px[p] < xmin) xmin = px[p];
			if (py[p] > ymax) ymax = py[p];
			if (py[p] < ymin) ymin = py[p];
		}

		double dx = K_ALPHA * (xmax - xmin);
		double dy = K_ALPHA * (ymax - ymin);

		head = pointCount;
		tail = pointCount + 1;
		px[head] = xmin - dx;
		py[head] = ymin - dy;
		px[tail] = xmax + dx;
		py[tail] = ymin - dy;

		var keys = order.AsSpan(0, orderCount);
		if (keys.Length > 1)
			introSort(keys, 2 * (System.Numerics.BitOperations.Log2((uint)keys.Length) + 1));
	}

	int compare(int a, int b) {
		double ay = py[a], by = py[b];
		if (ay < by) return -1;
		if (ay > by) return 1;
		double ax = px[a], bx = px[b];
		if (ax < bx) return -1;
		if (ax > bx) return 1;
		return 0;
	}

	// Copy of Array.Sort's introsort minus the delegate allocation
	// Has to stay identical or duplicate points sort differently

	void introSort(Span<int> keys, int depthLimit) {
		int size = keys.Length;
		while (size > 1) {
			if (size <= 16) {
				if (size == 2) {
					swapIfGreater(keys, 0, 1);
					return;
				}
				if (size == 3) {
					swapIfGreater(keys, 0, 1);
					swapIfGreater(keys, 0, 2);
					swapIfGreater(keys, 1, 2);
					return;
				}
				insertionSort(keys[..size]);
				return;
			}
			if (depthLimit == 0) {
				heapSort(keys[..size]);
				return;
			}
			depthLimit--;
			int p = pickPivotAndPartition(keys[..size]);
			introSort(keys[(p + 1)..size], depthLimit);
			size = p;
		}
	}

	void swapIfGreater(Span<int> keys, int i, int j) {
		if (compare(keys[i], keys[j]) > 0)
			(keys[i], keys[j]) = (keys[j], keys[i]);
	}

	int pickPivotAndPartition(Span<int> keys) {
		int hi = keys.Length - 1;
		int middle = hi >> 1;
		swapIfGreater(keys, 0, middle);
		swapIfGreater(keys, 0, hi);
		swapIfGreater(keys, middle, hi);

		int pivot = keys[middle];
		(keys[middle], keys[hi - 1]) = (keys[hi - 1], keys[middle]);
		int left = 0, right = hi - 1;
		while (left < right) {
			while (compare(keys[++left], pivot) < 0) ;
			while (compare(pivot, keys[--right]) < 0) ;
			if (left >= right) break;
			(keys[left], keys[right]) = (keys[right], keys[left]);
		}
		if (left != hi - 1)
			(keys[left], keys[hi - 1]) = (keys[hi - 1], keys[left]);
		return left;
	}

	void heapSort(Span<int> keys) {
		int n = keys.Length;
		for (int i = n >> 1; i >= 1; i--)
			downHeap(keys, i, n);
		for (int i = n; i > 1; i--) {
			(keys[0], keys[i - 1]) = (keys[i - 1], keys[0]);
			downHeap(keys, 1, i - 1);
		}
	}

	void downHeap(Span<int> keys, int i, int n) {
		int d = keys[i - 1];
		while (i <= n >> 1) {
			int child = 2 * i;
			if (child < n && compare(keys[child - 1], keys[child]) < 0)
				child++;
			if (!(compare(d, keys[child - 1]) < 0))
				break;
			keys[i - 1] = keys[child - 1];
			i = child;
		}
		keys[i - 1] = d;
	}

	void insertionSort(Span<int> keys) {
		for (int i = 0; i < keys.Length - 1; i++) {
			int t = keys[i + 1];
			int j = i;
			while (j >= 0 && compare(t, keys[j]) < 0) {
				keys[j + 1] = keys[j];
				j--;
			}
			keys[j + 1] = t;
		}
	}

	#region Triangles

	int newTriangle(int a, int b, int c) {
		if (triCount == interior.Length) {
			Array.Resize(ref interior, triCount * 2);
			Array.Resize(ref tp, triCount * 6);
			Array.Resize(ref tn, triCount * 6);
			Array.Resize(ref tc, triCount * 6);
			Array.Resize(ref td, triCount * 6);
		}

		int t = triCount++;
		int s = t * 3;
		tp[s] = a; tp[s + 1] = b; tp[s + 2] = c;
		tn[s] = tn[s + 1] = tn[s + 2] = -1;
		tc[s] = tc[s + 1] = tc[s + 2] = 0;
		td[s] = td[s + 1] = td[s + 2] = false;
		interior[t] = false;
		return t;
	}

	// -1 = no triangle, indexing with it throws
	int pt(int t, int i) => tp[t * 3 + i];

	static void checkIndex(int i) {
		if ((uint)i > 2) throw new IndexOutOfRangeException();
	}

	bool contains(int t, int p) => same(pt(t, 0), p) || same(pt(t, 1), p) || same(pt(t, 2), p);
	bool contains(int t, int p, int q) => contains(t, p) && contains(t, q);

	void markNeighbor(int t, int p1, int p2, int other) {
		int a = pt(t, 0), b = pt(t, 1), c = pt(t, 2);
		if ((same(p1, c) && same(p2, b)) || (same(p1, b) && same(p2, c)))
			tn[t * 3] = other;
		else if ((same(p1, a) && same(p2, c)) || (same(p1, c) && same(p2, a)))
			tn[t * 3 + 1] = other;
		else if ((same(p1, a) && same(p2, b)) || (same(p1, b) && same(p2, a)))
			tn[t * 3 + 2] = other;
	}

	void markNeighbor(int t, int other) {
		int a = pt(t, 0), b = pt(t, 1), c = pt(t, 2);
		if (contains(other, b, c)) {
			tn[t * 3] = other;
			markNeighbor(other, b, c, t);
		}
		else if (contains(other, a, c)) {
			tn[t * 3 + 1] = other;
			markNeighbor(other, a, c, t);
		}
		else if (contains(other, a, b)) {
			tn[t * 3 + 2] = other;
			markNeighbor(other, a, b, t);
		}
	}

	void clearNeighbors(int t) => tn[t * 3] = tn[t * 3 + 1] = tn[t * 3 + 2] = -1;
	void clearDelaunayEdges(int t) => td[t * 3] = td[t * 3 + 1] = td[t * 3 + 2] = false;

	// Point of t across from the edge it shares with other
	int oppositePoint(int t, int other, int p) => pointCW(t, pointCW(other, p));

	// rotates t clockwise around opoint
	void rotate(int t, int opoint, int npoint) {
		int s = t * 3;
		if (same(opoint, tp[s])) {
			tp[s + 1] = tp[s];
			tp[s] = tp[s + 2];
			tp[s + 2] = npoint;
		}
		else if (same(opoint, tp[s + 1])) {
			tp[s + 2] = tp[s + 1];
			tp[s + 1] = tp[s];
			tp[s] = npoint;
		}
		else if (same(opoint, tp[s + 2])) {
			tp[s] = tp[s + 2];
			tp[s + 2] = tp[s + 1];
			tp[s + 1] = npoint;
		}
		else
			Debug.Assert(false);
	}

	int index(int t, int p) {
		if (same(p, pt(t, 0))) return 0;
		if (same(p, pt(t, 1))) return 1;
		if (same(p, pt(t, 2))) return 2;
		Debug.Assert(false);
		return -1;
	}

	int edgeIndex(int t, int p1, int p2) {
		int a = pt(t, 0), b = pt(t, 1), c = pt(t, 2);
		if (same(a, p1)) {
			if (same(b, p2)) return 2;
			if (same(c, p2)) return 1;
		}
		else if (same(b, p1)) {
			if (same(c, p2)) return 0;
			if (same(a, p2)) return 2;
		}
		else if (same(c, p1)) {
			if (same(a, p2)) return 1;
			if (same(b, p2)) return 0;
		}
		return -1;
	}

	void markConstrainedEdge(int t, int p, int q) {
		int a = pt(t, 0), b = pt(t, 1), c = pt(t, 2);
		if ((same(q, a) && same(p, b)) || (same(q, b) && same(p, a)))
			tc[t * 3 + 2] |= eeKind;
		else if ((same(q, a) && same(p, c)) || (same(q, c) && same(p, a)))
			tc[t * 3 + 1] |= eeKind;
		else if ((same(q, b) && same(p, c)) || (same(q, c) && same(p, b)))
			tc[t * 3] |= eeKind;
	}

	int pointCW(int t, int p) {
		if (same(p, pt(t, 0))) return pt(t, 2);
		if (same(p, pt(t, 1))) return pt(t, 0);
		if (same(p, pt(t, 2))) return pt(t, 1);
		Debug.Assert(false);
		return -1;
	}

	int pointCCW(int t, int p) {
		if (same(p, pt(t, 0))) return pt(t, 1);
		if (same(p, pt(t, 1))) return pt(t, 2);
		if (same(p, pt(t, 2))) return pt(t, 0);
		Debug.Assert(false);
		return -1;
	}

	// Slot of the edge across from, clockwise from and counter-clockwise from p
	int across(int t, int p) => same(p, pt(t, 0)) ? 0 : same(p, pt(t, 1)) ? 1 : 2;
	int cwSlot(int t, int p) => same(p, pt(t, 0)) ? 1 : same(p, pt(t, 1)) ? 2 : 0;
	int ccwSlot(int t, int p) => same(p, pt(t, 0)) ? 2 : same(p, pt(t, 1)) ? 0 : 1;

	int neighborAcross(int t, int p) => tn[t * 3 + across(t, p)];
	int neighborCW(int t, int p) => tn[t * 3 + cwSlot(t, p)];
	int neighborCCW(int t, int p) => tn[t * 3 + ccwSlot(t, p)];

	#endregion

	#region Advancing front

	int newNode(int point, int triangle) {
		if (nodeCount == nodePoint.Length) {
			Array.Resize(ref nodePoint, nodeCount * 2);
			Array.Resize(ref nodeTri, nodeCount * 2);
			Array.Resize(ref nodeNext, nodeCount * 2);
			Array.Resize(ref nodePrev, nodeCount * 2);
		}
		int n = nodeCount++;
		nodePoint[n] = point;
		nodeTri[n] = triangle;
		nodeNext[n] = -1;
		nodePrev[n] = -1;
		return n;
	}

	double nodeX(int n) => px[nodePoint[n]];
	double nodeY(int n) => py[nodePoint[n]];

	void createAdvancingFront() {
		int t = newTriangle(order[0], head, tail);
		int afHead = newNode(head, t);
		int afMiddle = newNode(order[0], t);
		int afTail = newNode(tail, -1);
		frontHead = afHead;
		frontSearch = afHead;

		nodeNext[afHead] = afMiddle;
		nodeNext[afMiddle] = afTail;
		nodePrev[afMiddle] = afHead;
		nodePrev[afTail] = afMiddle;
	}

	int locateNode(double x) {
		int node = frontSearch;
		if (x < nodeX(node)) {
			while ((node = nodePrev[node]) != -1) {
				if (x >= nodeX(node)) {
					frontSearch = node;
					return node;
				}
			}
		}
		else {
			while ((node = nodeNext[node]) != -1) {
				if (x < nodeX(node)) {
					frontSearch = nodePrev[node];
					return nodePrev[node];
				}
			}
		}
		return -1;
	}

	int locatePoint(int point) {
		double x = px[point];
		int node = frontSearch;
		double nx = nodeX(node);

		if (x == nx) {
			if (point != nodePoint[node]) {
				// Two nodes can share an x for a moment
				if (same(point, nodePoint[nodePrev[node]]))
					node = nodePrev[node];
				else if (same(point, nodePoint[nodeNext[node]]))
					node = nodeNext[node];
				else
					Debug.Assert(false);
			}
		}
		else if (x < nx) {
			while ((node = nodePrev[node]) != -1)
				if (same(point, nodePoint[node]))
					break;
		}
		else {
			while ((node = nodeNext[node]) != -1)
				if (same(point, nodePoint[node]))
					break;
		}

		if (node != -1)
			frontSearch = node;
		return node;
	}

	void mapTriangleToNodes(int t) {
		for (int i = 0; i < 3; i++) {
			if (tn[t * 3 + i] == -1) {
				int n = locatePoint(pointCW(t, tp[t * 3 + i]));
				if (n != -1)
					nodeTri[n] = t;
			}
		}
	}

	#endregion

	#region Sweep

	void sweepPoints() {
		for (int i = 1; i < orderCount; i++) {
			int point = order[i];
			int node = pointEvent(point);
			for (int e = firstEdge[point]; e != -1; e = edgeNext[e])
				edgeEvent(e, node);
		}
	}

	void finalizationPolygon() {
		// Get an internal triangle to start with
		int t = nodeTri[nodeNext[frontHead]];
		int p = nodePoint[nodeNext[frontHead]];
		while (t != -1 && (tc[t * 3 + cwSlot(t, p)] & CONSTRAINED) == 0)
			t = neighborCCW(t, p);

		if (t != -1)
			meshClean(t);
	}

	// Floods out from t, only the outline and holes stop it
	void meshClean(int triangle) {
		int count = 0;
		stack[count++] = triangle;

		while (count != 0) {
			int t = stack[--count];
			if (t == -1 || interior[t]) continue;

			interior[t] = true;
			grow(ref result, resultCount + 1);
			result[resultCount++] = t;
			grow(ref stack, count + 3);
			for (int i = 0; i < 3; i++)
				if ((tc[t * 3 + i] & BOUNDARY) == 0)
					stack[count++] = tn[t * 3 + i];
		}
	}

	int pointEvent(int point) {
		int node = locateNode(px[point]);
		if (node == -1 || nodeNext[node] == -1)
			throw new Exception("PointEvent - null node");

		int created = newFrontTriangle(point, node);

		// Only need to check +epsilon since point never have smaller
		if (px[point] <= nodeX(node) + EPSILON)
			fill(node);

		fillAdvancingFront(created);
		return created;
	}

	void edgeEvent(int edge, int node) {
		int p = edgeP[edge], q = edgeQ[edge];
		eeP = p;
		eeQ = q;
		eeKind = edgeKind[edge];
		eeRight = px[p] > px[q];

		if (isEdgeSideOfTriangle(nodeTri[node], p, q))
			return;

		fillEdgeEvent(p, q, node);
		edgeEvent(p, q, nodeTri[node], q);
	}

	void edgeEvent(int ep, int eq, int triangle, int point) {
		if (triangle == -1)
			throw new Exception("EdgeEvent - null triangle");

		if (isEdgeSideOfTriangle(triangle, ep, eq))
			return;

		int p1 = pointCCW(triangle, point);
		int o1 = orient2d(eq, p1, ep);
		if (o1 == COLLINEAR) {
			if (!contains(triangle, eq, p1))
				throw new NotSupportedException("EdgeEvent - collinear points not supported");

			markConstrainedEdge(triangle, eq, p1);
			eeQ = p1;
			triangle = neighborAcross(triangle, point);
			edgeEvent(ep, p1, triangle, p1);
			return;
		}

		int p2 = pointCW(triangle, point);
		int o2 = orient2d(eq, p2, ep);
		if (o2 == COLLINEAR) {
			if (!contains(triangle, eq, p2))
				throw new NotSupportedException("EdgeEvent - collinear points not supported");

			markConstrainedEdge(triangle, eq, p2);
			eeQ = p2;
			triangle = neighborAcross(triangle, point);
			edgeEvent(ep, p2, triangle, p2);
			return;
		}

		if (o1 == o2) {
			// Rotate to a triangle that crosses the edge
			triangle = o1 == CW ? neighborCCW(triangle, point) : neighborCW(triangle, point);
			edgeEvent(ep, eq, triangle, point);
		}
		else
			flipEdgeEvent(ep, eq, triangle, point);
	}

	bool isEdgeSideOfTriangle(int t, int ep, int eq) {
		int i = edgeIndex(t, ep, eq);
		if (i == -1) return false;

		tc[t * 3 + i] |= eeKind;
		int other = tn[t * 3 + i];
		if (other != -1)
			markConstrainedEdge(other, ep, eq);
		return true;
	}

	int newFrontTriangle(int point, int node) {
		int t = newTriangle(point, nodePoint[node], nodePoint[nodeNext[node]]);
		markNeighbor(t, nodeTri[node]);

		int created = newNode(point, -1);
		nodeNext[created] = nodeNext[node];
		nodePrev[created] = node;
		nodePrev[nodeNext[node]] = created;
		nodeNext[node] = created;

		if (!legalize(t))
			mapTriangleToNodes(t);

		return created;
	}

	void fill(int node) {
		int prev = nodePrev[node], next = nodeNext[node];
		int t = newTriangle(nodePoint[prev], nodePoint[node], nodePoint[next]);
		markNeighbor(t, nodeTri[prev]);
		markNeighbor(t, nodeTri[node]);

		nodeNext[prev] = next;
		nodePrev[next] = prev;

		// If it was legalized the triangle has already been mapped.
		if (!legalize(t))
			mapTriangleToNodes(t);
	}

	void fillAdvancingFront(int n) {
		// Fill right holes
		int node = nodeNext[n];
		while (nodeNext[node] != -1) {
			if (largeHoleDontFill(node)) break;
			fill(node);
			node = nodeNext[node];
		}

		// Fill left holes
		node = nodePrev[n];
		while (nodePrev[node] != -1) {
			if (largeHoleDontFill(node)) break;
			fill(node);
			node = nodePrev[node];
		}

		// Fill right basins
		if (nodeNext[n] != -1 && nodeNext[nodeNext[n]] != -1) {
			if (basinAngle(n) < PI_3div4)
				fillBasin(n);
		}
	}

	bool largeHoleDontFill(int node) {
		int next = nodeNext[node], prev = nodePrev[node];
		int o = nodePoint[node];
		if (!angleExceeds90Degrees(o, nodePoint[next], nodePoint[prev]))
			return false;

		if (angle(o, nodePoint[next], nodePoint[prev]) < 0)
			return true;

		// Only want angles on the same side as the point being added
		int next2 = nodeNext[next];
		if (next2 != -1 && !angleExceedsPlus90DegreesOrIsNegative(o, nodePoint[next2], nodePoint[prev]))
			return false;

		int prev2 = nodePrev[prev];
		if (prev2 != -1 && !angleExceedsPlus90DegreesOrIsNegative(o, nodePoint[next], nodePoint[prev2]))
			return false;

		return true;
	}

	bool angleExceeds90Degrees(int origin, int pa, int pb) {
		double a = angle(origin, pa, pb);
		return a > PI_div2 || a < -PI_div2;
	}

	bool angleExceedsPlus90DegreesOrIsNegative(int origin, int pa, int pb) {
		double a = angle(origin, pa, pb);
		return a > PI_div2 || a < 0;
	}

	double angle(int origin, int pa, int pb) {
		double ox = px[origin];
		double oy = py[origin];
		double ax = px[pa] - ox;
		double ay = py[pa] - oy;
		double bx = px[pb] - ox;
		double by = py[pb] - oy;
		double x = ax * by - ay * bx;
		double y = ax * bx + ay * by;
		return Math.Atan2(x, y);
	}

	double basinAngle(int node) {
		int p = nodePoint[node], p2 = nodePoint[nodeNext[nodeNext[node]]];
		double ax = px[p] - px[p2];
		double ay = py[p] - py[p2];
		return Math.Atan2(ay, ax);
	}

	bool legalize(int t) {
		for (int i = 0; i < 3; i++) {
			if (td[t * 3 + i])
				continue;

			int other = tn[t * 3 + i];
			if (other == -1)
				continue;

			int p = tp[t * 3 + i];
			int op = oppositePoint(other, t, p);
			int oi = index(other, op);
			checkIndex(oi);

			// Constrained edges, and delaunay edges during recursive legalization, stay put.
			if ((tc[other * 3 + oi] & CONSTRAINED) != 0 || td[other * 3 + oi]) {
				tc[t * 3 + i] = tc[other * 3 + oi];
				continue;
			}

			if (incircle(p, pointCCW(t, p), pointCW(t, p), op)) {
				td[t * 3 + i] = true;
				td[other * 3 + oi] = true;

				rotateTrianglePair(t, p, other, op);

				// Map each triangle to the front only once
				if (!legalize(t))
					mapTriangleToNodes(t);
				if (!legalize(other))
					mapTriangleToNodes(other);

				// Only delaunay until a new triangle or point is added
				td[t * 3 + i] = false;
				td[other * 3 + oi] = false;
				return true;
			}
		}

		return false;
	}

	// true if d is inside the circle through a, b and c. a and d are on opposite sides of bc.
	bool incircle(int pa, int pb, int pc, int pd) {
		double adx = px[pa] - px[pd];
		double ady = py[pa] - py[pd];
		double bdx = px[pb] - px[pd];
		double bdy = py[pb] - py[pd];

		double adxbdy = adx * bdy;
		double bdxady = bdx * ady;
		double oabd = adxbdy - bdxady;

		if (oabd <= 0)
			return false;

		double cdx = px[pc] - px[pd];
		double cdy = py[pc] - py[pd];

		double cdxady = cdx * ady;
		double adxcdy = adx * cdy;
		double ocad = cdxady - adxcdy;

		if (ocad <= 0)
			return false;

		double bdxcdy = bdx * cdy;
		double cdxbdy = cdx * bdy;

		double alift = adx * adx + ady * ady;
		double blift = bdx * bdx + bdy * bdy;
		double clift = cdx * cdx + cdy * cdy;

		double det = alift * (bdxcdy - cdxbdy) + blift * ocad + clift * oabd;

		return det > 0;
	}

	// rotates the pair one vertex clockwise
	void rotateTrianglePair(int t, int p, int ot, int op) {
		int n1 = neighborCCW(t, p);
		int n2 = neighborCW(t, p);
		int n3 = neighborCCW(ot, op);
		int n4 = neighborCW(ot, op);

		byte ce1 = tc[t * 3 + ccwSlot(t, p)];
		byte ce2 = tc[t * 3 + cwSlot(t, p)];
		byte ce3 = tc[ot * 3 + ccwSlot(ot, op)];
		byte ce4 = tc[ot * 3 + cwSlot(ot, op)];

		bool de1 = td[t * 3 + ccwSlot(t, p)];
		bool de2 = td[t * 3 + cwSlot(t, p)];
		bool de3 = td[ot * 3 + ccwSlot(ot, op)];
		bool de4 = td[ot * 3 + cwSlot(ot, op)];

		rotate(t, p, op);
		rotate(ot, op, p);

		td[ot * 3 + ccwSlot(ot, p)] = de1;
		td[t * 3 + cwSlot(t, p)] = de2;
		td[t * 3 + ccwSlot(t, op)] = de3;
		td[ot * 3 + cwSlot(ot, op)] = de4;

		tc[ot * 3 + ccwSlot(ot, p)] = ce1;
		tc[t * 3 + cwSlot(t, p)] = ce2;
		tc[t * 3 + ccwSlot(t, op)] = ce3;
		tc[ot * 3 + cwSlot(ot, op)] = ce4;

		clearNeighbors(t);
		clearNeighbors(ot);
		if (n1 != -1) markNeighbor(ot, n1);
		if (n2 != -1) markNeighbor(t, n2);
		if (n3 != -1) markNeighbor(t, n3);
		if (n4 != -1) markNeighbor(ot, n4);
		markNeighbor(t, ot);
	}

	void fillBasin(int node) {
		int next = nodeNext[node];
		if (orient2d(nodePoint[node], nodePoint[next], nodePoint[nodeNext[next]]) == CCW)
			basinLeft = nodeNext[next];
		else
			basinLeft = next;

		// Find the bottom and right node
		basinBottom = basinLeft;
		while (nodeNext[basinBottom] != -1 && nodeY(basinBottom) >= nodeY(nodeNext[basinBottom]))
			basinBottom = nodeNext[basinBottom];
		if (basinBottom == basinLeft)
			return;

		basinRight = basinBottom;
		while (nodeNext[basinRight] != -1 && nodeY(basinRight) < nodeY(nodeNext[basinRight]))
			basinRight = nodeNext[basinRight];
		if (basinRight == basinBottom)
			return;

		basinWidth = nodeX(basinRight) - nodeX(basinLeft);
		basinLeftHighest = nodeY(basinLeft) > nodeY(basinRight);

		fillBasinReq(basinBottom);
	}

	void fillBasinReq(int node) {
		while (!isShallow(node)) {
			fill(node);

			int prev = nodePrev[node], next = nodeNext[node];
			if (prev == basinLeft && next == basinRight)
				return;

			if (prev == basinLeft) {
				if (orient2d(nodePoint[node], nodePoint[next], nodePoint[nodeNext[next]]) == CW)
					return;
				node = next;
			}
			else if (next == basinRight) {
				if (orient2d(nodePoint[node], nodePoint[prev], nodePoint[nodePrev[prev]]) == CCW)
					return;
				node = prev;
			}
			else {
				// Continue with the neighbor node with lowest Y value
				node = nodeY(prev) < nodeY(next) ? prev : next;
			}
		}
	}

	bool isShallow(int node) {
		double height = basinLeftHighest ? nodeY(basinLeft) - nodeY(node) : nodeY(basinRight) - nodeY(node);

		return basinWidth > height;
	}

	void fillEdgeEvent(int ep, int eq, int node) {
		if (eeRight)
			fillRightAboveEdgeEvent(ep, eq, node);
		else
			fillLeftAboveEdgeEvent(ep, eq, node);
	}

	void fillRightAboveEdgeEvent(int ep, int eq, int node) {
		while (nodeX(nodeNext[node]) < px[ep]) {
			// Check if next node is below the edge
			if (orient2d(eq, nodePoint[nodeNext[node]], ep) == CCW)
				fillRightBelowEdgeEvent(ep, eq, node);
			else
				node = nodeNext[node];
		}
	}

	void fillRightBelowEdgeEvent(int ep, int eq, int node) {
		while (nodeX(node) < px[ep]) {
			int next = nodeNext[node];
			if (orient2d(nodePoint[node], nodePoint[next], nodePoint[nodeNext[next]]) == CCW) {
				fillRightConcaveEdgeEvent(ep, eq, node);
				return;
			}

			fillRightConvexEdgeEvent(ep, eq, node);
		}
	}

	void fillRightConcaveEdgeEvent(int ep, int eq, int node) {
		fill(nodeNext[node]);
		if (nodePoint[nodeNext[node]] != ep) {
			// Next above or below edge?
			if (orient2d(eq, nodePoint[nodeNext[node]], ep) == CCW) {
				// Below, and next is concave
				int next = nodeNext[node];
				if (orient2d(nodePoint[node], nodePoint[next], nodePoint[nodeNext[next]]) == CCW)
					fillRightConcaveEdgeEvent(ep, eq, node);
			}
		}
	}

	void fillRightConvexEdgeEvent(int ep, int eq, int node) {
		int next = nodeNext[node], next2 = nodeNext[next];
		// Next concave or convex?
		if (orient2d(nodePoint[next], nodePoint[next2], nodePoint[nodeNext[next2]]) == CCW)
			fillRightConcaveEdgeEvent(ep, eq, next);
		// Convex, and next is below the edge
		else if (orient2d(eq, nodePoint[next2], ep) == CCW)
			fillRightConvexEdgeEvent(ep, eq, next);
	}

	void fillLeftAboveEdgeEvent(int ep, int eq, int node) {
		while (nodeX(nodePrev[node]) > px[ep]) {
			// Check if next node is below the edge
			if (orient2d(eq, nodePoint[nodePrev[node]], ep) == CW)
				fillLeftBelowEdgeEvent(ep, eq, node);
			else
				node = nodePrev[node];
		}
	}

	void fillLeftBelowEdgeEvent(int ep, int eq, int node) {
		while (nodeX(node) > px[ep]) {
			int prev = nodePrev[node];
			if (orient2d(nodePoint[node], nodePoint[prev], nodePoint[nodePrev[prev]]) == CW) {
				fillLeftConcaveEdgeEvent(ep, eq, node);
				return;
			}

			fillLeftConvexEdgeEvent(ep, eq, node);
		}
	}

	void fillLeftConvexEdgeEvent(int ep, int eq, int node) {
		int prev = nodePrev[node], prev2 = nodePrev[prev];
		// Next concave or convex?
		if (orient2d(nodePoint[prev], nodePoint[prev2], nodePoint[nodePrev[prev2]]) == CW)
			fillLeftConcaveEdgeEvent(ep, eq, prev);
		// Convex, and next is below the edge
		else if (orient2d(eq, nodePoint[prev2], ep) == CW)
			fillLeftConvexEdgeEvent(ep, eq, prev);
	}

	void fillLeftConcaveEdgeEvent(int ep, int eq, int node) {
		fill(nodePrev[node]);
		if (nodePoint[nodePrev[node]] != ep) {
			// Next above or below edge?
			if (orient2d(eq, nodePoint[nodePrev[node]], ep) == CW) {
				// Below, and next is concave
				int prev = nodePrev[node];
				if (orient2d(nodePoint[node], nodePoint[prev], nodePoint[nodePrev[prev]]) == CW)
					fillLeftConcaveEdgeEvent(ep, eq, node);
			}
		}
	}

	void flipEdgeEvent(int ep, int eq, int t, int p) {
		while (true) {
			int ot = neighborAcross(t, p);
			if (ot == -1)
				throw new Exception("FlipEdgeEvent - null neighbor across");

			int op = oppositePoint(ot, t, p);

			if (!inScanArea(p, pointCCW(t, p), pointCW(t, p), op)) {
				int newP = nextFlipPoint(ep, eq, ot, op);
				flipScanEdgeEvent(ep, eq, t, ot, newP);
				edgeEvent(ep, eq, t, p);
				return;
			}

			rotateTrianglePair(t, p, ot, op);
			mapTriangleToNodes(t);
			mapTriangleToNodes(ot);

			if (same(p, eq) && same(op, ep)) {
				if (same(eq, eeQ) && same(ep, eeP)) {
					markConstrainedEdge(t, ep, eq);
					markConstrainedEdge(ot, ep, eq);
					legalize(t);
					legalize(ot);
				}
				return;
			}

			int o = orient2d(eq, op, ep);
			t = nextFlipTriangle(o, t, ot, p, op);
		}
	}

	int nextFlipTriangle(int o, int t, int ot, int p, int op) {
		if (o == CCW) {
			// ot is not crossing edge after flip
			int oi = edgeIndex(ot, p, op);
			checkIndex(oi);
			td[ot * 3 + oi] = true;
			legalize(ot);
			clearDelaunayEdges(ot);
			return t;
		}

		// t is not crossing edge after flip
		int ti = edgeIndex(t, p, op);
		checkIndex(ti);
		td[t * 3 + ti] = true;
		legalize(t);
		clearDelaunayEdges(t);
		return ot;
	}

	int nextFlipPoint(int ep, int eq, int ot, int op) {
		int o = orient2d(eq, op, ep);
		if (o == CW) return pointCCW(ot, op);
		if (o == CCW) return pointCW(ot, op);
		throw new NotSupportedException("[Unsupported] Opposing point on constrained edge");
	}

	void flipScanEdgeEvent(int ep, int eq, int flipTriangle, int t, int p) {
		while (true) {
			int ot = neighborAcross(t, p);
			if (ot == -1)
				throw new Exception("FlipScanEdgeEvent - null neighbor across");

			int op = oppositePoint(ot, t, p);
			if (op == -1)
				throw new Exception("FlipScanEdgeEvent - null opposing point");

			int p1 = pointCCW(flipTriangle, eq);
			int p2 = pointCW(flipTriangle, eq);
			if (p1 == -1 || p2 == -1)
				throw new Exception("FlipScanEdgeEvent - null on either of points");

			if (inScanArea(eq, p1, p2, op)) {
				// flip with new edge op->eq
				flipEdgeEvent(eq, op, ot, op);
				return;
			}

			p = nextFlipPoint(ep, eq, ot, op);
			t = ot;
		}
	}

	int orient2d(int pa, int pb, int pc) {
		double detleft = (px[pa] - px[pc]) * (py[pb] - py[pc]);
		double detright = (py[pa] - py[pc]) * (px[pb] - px[pc]);
		double val = detleft - detright;

		if (val == 0.0 || val == -0.0)
			return COLLINEAR;
		if (val > 0)
			return CCW;
		return CW;
	}

	bool inScanArea(int pa, int pb, int pc, int pd) {
		double oadb = (px[pa] - px[pb]) * (py[pd] - py[pb]) - (px[pd] - px[pb]) * (py[pa] - py[pb]);
		if (oadb >= -EPSILON)
			return false;

		double oadc = (px[pa] - px[pc]) * (py[pd] - py[pc]) - (px[pd] - px[pc]) * (py[pa] - py[pc]);
		if (oadc <= EPSILON)
			return false;

		return true;
	}

	#endregion
}
