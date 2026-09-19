using Nucleus.Models.Runtime;
using Nucleus.Rendering;
using Nucleus.Types;
using Poly2Tri;
using Raylib_cs;
using System.Buffers;

namespace Nucleus.Models;

public interface IClipPolygon<in SlotType>
{
	int GetVerticesCount();

	int ComputeWorldVerticesInto(SlotType slot, Vector2F[] into);
}

// Since clipping is done via GPU stencils, all that's really needed here is some generics for slot logic.
// This lets EditorModel and ModelInstance use pretty much the exact same logic with no changes needed
public abstract class ModelClipper<ModelType, BoneType, SlotType, ClipAttachmentType>(ModelType model, bool flipY)
	where ModelType : IModelInterface<BoneType, SlotType>
	where BoneType : class
	where SlotType : class
	where ClipAttachmentType : class, IClipPolygon<SlotType>
{
	public bool Active { get; protected set; }
	public ModelType Model = model;

	private Vector2F[]? _clipPolygon;

	public bool FlipY { get; set; } = flipY;

	public void End() {
		bool renderMask = false;
		switch ((M4S_StencilMode)Model4System.m4s_stencilmode.GetInt()) {
			case M4S_StencilMode.On:
			default:
				break;

			case M4S_StencilMode.Off:
				return;

			case M4S_StencilMode.RenderMask:
				renderMask = true;
				break;
		}

		if (Active && !renderMask)
			Stencils.End();

		Active = false;
		_endAt = null;
		_workingAttachment = null;

		if (_clipPolygon == null)
			return;

		ArrayPool<Vector2F>.Shared.Return(_clipPolygon, true);
		_clipPolygon = null;

		// Do not clear triangles or Points because it now intentionally persists between frames for caching

		// triangles.Clear();
		// shape.Points.Clear();
	}

	private SlotType? _endAt;
	private ClipAttachmentType? _workingAttachment;
	private int _verticesLength;
	private readonly Shape _shape = new Shape();
	private readonly List<Triangle> _triangles = [];

	private Vector2F[]? _cachedVertices;
	private int _cachedVerticesLength;
	private ClipAttachmentType? _cachedAttachment;
	private bool _hasCachedTriangulation;

	public void Start(ClipAttachmentType attachment, SlotType slot, string? endAt = null) {
		if (_workingAttachment != null) return;

		bool renderMask = false;
		switch ((M4S_StencilMode)Model4System.m4s_stencilmode.GetInt()) {
			case M4S_StencilMode.On:
			default:
				break;

			case M4S_StencilMode.Off:
				return;

			case M4S_StencilMode.RenderMask:
				renderMask = true;
				break;
		}

		Active = true;
		_workingAttachment = attachment;
		_endAt = endAt == null ? null : Model.FindSlot(endAt);

		_clipPolygon = ArrayPool<Vector2F>.Shared.Rent(attachment.GetVerticesCount());
		_verticesLength = attachment.ComputeWorldVerticesInto(slot, _clipPolygon);

		bool isCached =
			_hasCachedTriangulation &&
			ReferenceEquals(_cachedAttachment, attachment) &&
			_cachedVerticesLength == _verticesLength &&
			_cachedVertices != null &&
			_clipPolygon.AsSpan(0, _verticesLength).SequenceEqual(_cachedVertices.AsSpan(0, _verticesLength));

		if (!isCached) {
			BuildNonDegeneratePolygon(_clipPolygon, _verticesLength, _shape.Points);

			_triangles.Clear();
			_shape.Triangulate(_triangles);

			// Update the cache
			if (_cachedVertices == null || _cachedVertices.Length < _verticesLength) {
				if (_cachedVertices != null)
					ArrayPool<Vector2F>.Shared.Return(_cachedVertices);
				_cachedVertices = ArrayPool<Vector2F>.Shared.Rent(_verticesLength);
			}

			_clipPolygon.AsSpan(0, _verticesLength).CopyTo(_cachedVertices);
			_cachedVerticesLength = _verticesLength;
			_cachedAttachment = attachment;
			_hasCachedTriangulation = true;
		}

		// Draw stencil mask
		if (!renderMask) {
			Stencils.Begin();
			Stencils.BeginMask();
		}

		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.Color4ub(255, 255, 255, 255);

		foreach (Triangle triangle in _triangles) {
			TriPoint a = triangle.Points[0], b = triangle.Points[1], c = triangle.Points[2];
			if (FlipY) {
				Rlgl.Vertex2f((float)a.X, -(float)a.Y);
				Rlgl.Vertex2f((float)b.X, -(float)b.Y);
				Rlgl.Vertex2f((float)c.X, -(float)c.Y);
			}
			else {
				Rlgl.Vertex2f((float)a.X, (float)a.Y);
				Rlgl.Vertex2f((float)b.X, (float)b.Y);
				Rlgl.Vertex2f((float)c.X, (float)c.Y);
			}
		}

		Rlgl.End();
		if (!renderMask)
			Stencils.EndMask();
	}

	private const double DuplicateEpsilon = 1e-4;
	private const double CollinearEpsilon = 1e-3;

	private static void BuildNonDegeneratePolygon(Vector2F[] source, int length, List<TriPoint> into) {
		into.Clear();

		Span<Vector2F> pts = length <= 256 ? stackalloc Vector2F[length] : new Vector2F[length];
		int count = 0;
		for (int i = 0; i < length; i++) {
			Vector2F v = source[i];
			if (count > 0 && NearlyEqual(pts[count - 1], v))
				continue;
			pts[count++] = v;
		}
		if (count > 1 && NearlyEqual(pts[count - 1], pts[0]))
			count--;

		if (count < 3)
			return; 
		into.EnsureCapacity(count);

		for (int i = 0; i < count; i++) {
			Vector2F prev = pts[(i - 1 + count) % count];
			Vector2F cur = pts[i];
			Vector2F next = pts[(i + 1) % count];

			double ex = next.X - prev.X, ey = next.Y - prev.Y;
			double area2 = Math.Abs((cur.X - prev.X) * ey - (cur.Y - prev.Y) * ex);
			double edgeLen = Math.Sqrt(ex * ex + ey * ey);
			if (edgeLen > 0 && area2 <= CollinearEpsilon * edgeLen)
				continue;

			into.Add(new TriPoint(cur.X, cur.Y));
		}

		if (into.Count < 3)
			into.Clear();
	}

	private static bool NearlyEqual(Vector2F a, Vector2F b) {
		double dx = a.X - b.X, dy = a.Y - b.Y;
		return dx * dx + dy * dy <= DuplicateEpsilon * DuplicateEpsilon;
	}

	public void NextSlot(SlotType slot) {
		if (!Active) return;
		if (_endAt == null) return;

		if (_endAt == slot) {
			End();
		}
	}
}