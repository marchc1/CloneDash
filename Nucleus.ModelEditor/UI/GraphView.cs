using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI;

public class GraphView : BaseTimelineView
{
	public override string Name => "Graph Editor";
	public override bool LockDragDirection => false;

	private const float ROW_HEIGHT = DopesheetView.ROW_HEIGHT;
	private const float KEY_SIZE = 7;
	private const float HIT = 6;
	private const float HANDLE_RADIUS = 4;
	private const float DRAG_START = 4;
	private const float AXIS_SNAP = 8;
	private const float DOT_SPACE = 28;
	private const double DOUBLE_CLICK = 0.3;
	private const double BOX_KEEP_PAUSE = 0.25;
	// With more bones than this shown and nothing selected, only the first bone starts visible
	private const int AUTO_VISIBLE_GROUPS = 3;

	public class GraphCurve
	{
		public required TimelineRow Row;
		public required FCurve<float> Curve;
		public required int Index;
		public required Color Color;
	}

	public readonly record struct Hit(GraphCurve Curve, KeyframeRef<float> Key, int Side);

	public AnimationRows Rows { get; } = new() { HideOtherRowsOnSelection = false };
	/// <summary>
	/// Set by clicks in the graph
	/// </summary>
	public bool Focused { get; set; }
	/// <summary>
	/// Auto-frames the visible curves
	/// </summary>
	public bool AutoFrame { get; set; } = true;
	/// <summary>
	/// Value at the vertical middle of the curve area
	/// </summary>
	public double ValueCenter { get; set; }
	/// <summary>
	/// Pixels per unit of value
	/// </summary>
	public double ValueZoom { get; set; } = 1;

	public IReadOnlyList<GraphCurve> Curves { get {
		RefreshCurves();
		return curves;
	} }
	public IReadOnlyCollection<EditorTimeline> Visible => visible;
	public IReadOnlyCollection<(KeyframeRef<float> Key, bool Right)> SelectedHandles => selectedHandles;

	static TimelineManager Timeline => ModelEditor.Active.File.Timeline;
	static double FPS => Timeline.GetReferenceFPS();

	readonly NamesPanel names;
	readonly CurvePanel curvePanel;

	readonly HashSet<EditorTimeline> visible = [];
	readonly HashSet<(KeyframeRef<float> Key, bool Right)> selectedHandles = [];
	readonly List<GraphCurve> curves = [];
	readonly HashSet<IFCurve> visibleCurves = [];
	int curvesVersion = -1;
	int visibilityVersion;
	int curvesVisibility = -1;
	readonly List<EditorTimeline> autoKey = [];
	EditorAnimation? autoAnimation;
	bool autoLocked;
	int autoFramedVisibility = -1;
	double autoMin, autoMax;

	public GraphView(Element parent) : base(parent) {
		curvePanel = new(this);
		curvePanel.Dock = Dock.Fill;
		curvePanel.DockMargin = RectangleF.Zero;
		curvePanel.SetPaintBackgroundEnabled(false);
		curvePanel.SetPaintBorderEnabled(false);

		names = new(this, ButtonsAndNames);
		names.Dock = Dock.Fill;
		names.DockMargin = RectangleF.Zero;
		names.SetPaintBackgroundEnabled(false);
		names.SetPaintBorderEnabled(false);

		BuildToolbar();
		KeyframeOverlay.MoveToFront();

		ModelEditor.Active.File.KeyframeCreated += (_, _, _) => Rows.MarkDirty();
		ModelEditor.Active.File.KeyframeRemoved += (_, _, _) => Rows.MarkDirty();
	}

	public override void CreateChannels() {
		Rows.MarkDirty();
		lastClick = null;
	}

	protected override float MaxScrollOffset => Math.Max(0, Rows.Shown.Count * ROW_HEIGHT - (names?.GetRenderBounds().H ?? 0) + ROW_HEIGHT);
	// Handles in one frame segments need to get this close
	protected override double ZoomLimit => 20000;
	float curveHeight => curvePanel?.GetRenderBounds().H ?? 0;
	float curveWidth => curvePanel?.GetRenderBounds().W ?? 0;

	// Toolbar

	private void BuildToolbar() {
		TopButtonPanel.Size = new(0, ToolRows.ROW_HEIGHT * 2 + ToolRows.GAP + 12);

		var top = DopesheetView.ToolbarRow(TopButtonPanel, 0);
		DopesheetView.LockButtons(top, Rows);
		DopesheetView.Space(top, 12);
		DopesheetView.TextButton(top, "Frame", 76, FrameCurves);
		DopesheetView.ToggleButton(top, "Auto", 64, () => AutoFrame, enabled => {
			AutoFrame = enabled;
			if (enabled)
				autoFramedVisibility = -1;
		});

		var bottom = DopesheetView.ToolbarRow(TopButtonPanel, ToolRows.GAP);
		DopesheetView.KeyButtons(bottom, Rows);
		DopesheetView.Space(bottom, 12);
		Preset(bottom, "Automatic", 104, AnimationRows.HandlePreset.Automatic);
		Preset(bottom, "Separate", 94, AnimationRows.HandlePreset.Separate);
		Preset(bottom, "Flat", 56, AnimationRows.HandlePreset.Flat);
		Preset(bottom, "Bounce", 82, AnimationRows.HandlePreset.Bounce);
		Preset(bottom, "Ease Out", 90, AnimationRows.HandlePreset.EaseOut);
		Preset(bottom, "Ease In", 80, AnimationRows.HandlePreset.EaseIn);
	}

	private void Preset(Panel row, string text, float width, AnimationRows.HandlePreset preset) {
		var button = DopesheetView.TextButton(row, text, width, () => Rows.ApplyPreset(preset));
		button.Thinking += (s) => s.SetMouseInputEnabled(ModelEditor.Active.KeyframesSelected);
	}

	// Visibility

	public bool IsVisible(TimelineRow row) => Rows.PropertiesUnder(row).Any(r => visible.Contains(r.Timeline!));

	/// <summary>
	/// Shows only the curves under the row, or toggles them with <paramref name="multiple"/>.
	/// </summary>
	public void ClickDot(TimelineRow row, bool multiple) {
		List<EditorTimeline> targets = [];
		foreach (var property in Rows.PropertiesUnder(row))
			targets.Add(property.Timeline!);
		if (multiple) {
			if (targets.All(visible.Contains))
				visible.ExceptWith(targets);
			else
				visible.UnionWith(targets);
		}
		else {
			visible.Clear();
			visible.UnionWith(targets);
		}
		visibilityVersion++;
	}

	// unlocked, the dots follow the rows
	// With many bones and nothing selected only the first bone is shown
	private void UpdateAutoVisibility() {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		var all = Rows.All;
		List<EditorTimeline> properties = [];
		foreach (var row in all) {
			if (row.Kind == RowKind.Property)
				properties.Add(row.Timeline!);
		}
		if (animation == autoAnimation && Rows.Locked == autoLocked && properties.SequenceEqual(autoKey)) return;
		bool wasLocked = autoLocked;
		autoAnimation = animation;
		autoLocked = Rows.Locked;
		autoKey.Clear();
		autoKey.AddRange(properties);
		if (Rows.Locked && wasLocked) return;

		visible.Clear();
		List<TimelineRow> groups = [];
		foreach (var row in all) {
			if (row.Kind == RowKind.Group)
				groups.Add(row);
		}
		if (Rows.FollowingSelection || groups.Count <= AUTO_VISIBLE_GROUPS)
			visible.UnionWith(autoKey);
		else if (groups.Count > 0)
			visible.UnionWith(Rows.PropertiesUnder(groups[0]).Select(r => r.Timeline!));
		visibilityVersion++;
	}

	private static Color CurveColor(EditorTimeline timeline, int index, int count) {
		if (timeline is SlotColorAlphaTimeline)
			return new(225, 225, 225);
		if (timeline is SlotTwoColorTimeline)
			return index switch {
				0 => new(235, 85, 85),
				1 => new(95, 210, 95),
				2 => new(95, 145, 255),
				3 => new(225, 225, 225),
				4 => new(150, 50, 50),
				5 => new(55, 130, 55),
				6 => new(55, 85, 160),
				_ => new(225, 225, 225)
			};
		if (timeline is SlotColorTimeline or SlotColorRGBTimeline)
			return index switch {
				0 => new(235, 85, 85),
				1 => new(95, 210, 95),
				2 => new(95, 145, 255),
				_ => new(225, 225, 225)
			};
		var color = timeline.Color;
		return count > 1 && index == 1 ? color.Adjust(0, -0.35f, 0.3f) : color;
	}

	private void RefreshCurves() {
		Rows.Refresh();
		UpdateAutoVisibility();
		if (curvesVersion == Rows.Version && curvesVisibility == visibilityVersion) return;
		curvesVersion = Rows.Version;
		curvesVisibility = visibilityVersion;
		curves.Clear();
		visibleCurves.Clear();
		foreach (var row in Rows.All) {
			if (row.Kind != RowKind.Property || !visible.Contains(row.Timeline!) || row.Timeline is DeformTimeline) continue;
			int count = row.Curves.Count(c => c is FCurve<float>);
			for (int i = 0; i < row.Curves.Length; i++) {
				if (row.Curves[i] is FCurve<float> curve && curve.Count > 0) {
					curves.Add(new GraphCurve {
						Row = row,
						Curve = curve,
						Index = i,
						Color = CurveColor(row.Timeline!, i, count)
					});
					visibleCurves.Add(curve);
				}
			}
		}
	}

	// Value axis

	public float ValueToY(double value) => (float)(curveHeight / 2 - (value - ValueCenter) * ValueZoom);
	public double YToValue(float y) => ValueCenter + (curveHeight / 2 - y) / ValueZoom;
	private float TimeToX(double time) => (float)FrameToX(time * FPS);

	public Vector2F KeyScreenPosition(KeyframeRef<float> key) => curvePanel.GetGlobalPosition() + new Vector2F(TimeToX(key.Time), ValueToY(key.Value));
	public Vector2F HandleScreenPosition(KeyframeHandle<float> handle)
		=> curvePanel.GetGlobalPosition() + new Vector2F(TimeToX(handle.Time), ValueToY(handle.Value));
	public Vector2F CurveScreenPosition(double time, double value) => curvePanel.GetGlobalPosition() + new Vector2F(TimeToX(time), ValueToY(value));
	public RectangleF CurveScreenBounds {
		get {
			var pos = curvePanel.GetGlobalPosition();
			var size = curvePanel.GetRenderBounds();
			return RectangleF.XYWH(pos.X, pos.Y, size.W, size.H);
		}
	}
	public Vector2F NameScreenPosition(TimelineRow row, float x)
		=> names.GetGlobalPosition() + new Vector2F(x, Rows.IndexOf(row) * ROW_HEIGHT - ScrollOffset + ROW_HEIGHT / 2);
	public Vector2F DotScreenPosition(TimelineRow row) => NameScreenPosition(row, names.GetRenderBounds().W - DOT_SPACE / 2);

	// keys, plus handles of bezier segments
	private bool Bounds(bool selectedOnly, out double minTime, out double maxTime, out double minValue, out double maxValue) {
		minTime = minValue = double.MaxValue;
		maxTime = maxValue = double.MinValue;
		bool any = false;
		foreach (var curve in Curves) {
			var keys = curve.Curve.Keyframes;
			for (int i = 0; i < keys.Count; i++) {
				var key = keys[i];
				bool keySelected = ModelEditor.Active.IsKeyframeSelected(curve.Curve.RefAt(i));
				if (!selectedOnly || keySelected) {
					Include(key.Time, key.Value, ref minTime, ref maxTime, ref minValue, ref maxValue);
					any = true;
				}
				if (key.Interpolation == KeyframeInterpolation.Bezier && i + 1 < keys.Count) {
					if (key.RightHandle is KeyframeHandle<float> rightHandle && (!selectedOnly || selectedHandles.Contains((curve.Curve.RefAt(i), true)))) {
						Include(rightHandle.Time, rightHandle.Value, ref minTime, ref maxTime, ref minValue, ref maxValue);
						any = true;
					}
					if (keys[i + 1].LeftHandle is KeyframeHandle<float> leftHandle && (!selectedOnly || selectedHandles.Contains((curve.Curve.RefAt(i + 1), false)))) {
						Include(leftHandle.Time, leftHandle.Value, ref minTime, ref maxTime, ref minValue, ref maxValue);
						any = true;
					}
				}
			}
		}
		return any;
	}

	private static void Include(double time, double value, ref double minTime, ref double maxTime, ref double minValue, ref double maxValue) {
		minTime = Math.Min(minTime, time);
		maxTime = Math.Max(maxTime, time);
		minValue = Math.Min(minValue, value);
		maxValue = Math.Max(maxValue, value);
	}

	private void FitValues(double minValue, double maxValue) {
		double span = maxValue - minValue;
		if (span < 1e-4)
			span = Math.Max(Math.Abs(minValue) * 0.5, 1);
		ValueZoom = Math.Max(1e-6, curveHeight * 0.8 / span);
		ValueCenter = (minValue + maxValue) / 2;
	}

	/// <summary>
	/// Fits the selected keys and handles, or every visible curve.
	/// </summary>
	public void FrameCurves() {
		if (curveHeight <= 0) return;
		if (!Bounds(true, out var minTime, out var maxTime, out var minValue, out var maxValue) && !Bounds(false, out minTime, out maxTime, out minValue, out maxValue)) return;
		FitValues(minValue, maxValue);
		double startFrame = minTime * FPS, endFrame = maxTime * FPS;
		double span = Math.Max(endFrame - startFrame, 10);
		Zoom = curveWidth * 0.8 / span;
		FrameOffset = defaultXOffset + (startFrame + endFrame) / 2 * Zoom - curveWidth / 2;
	}

	// Only adjusts when other curves are shown, or an edit pushes them out of view
	private void DoAutoFrame() {
		if (!AutoFrame || mode != DragMode.None || curveHeight <= 0) return;
		if (!Bounds(false, out _, out _, out var minValue, out var maxValue)) return;
		bool shown = autoFramedVisibility != visibilityVersion;
		bool edited = minValue != autoMin || maxValue != autoMax;
		autoMin = minValue;
		autoMax = maxValue;
		bool outside = ValueToY(maxValue) < 0 || ValueToY(minValue) > curveHeight;
		if (!shown && !(edited && outside)) return;
		autoFramedVisibility = visibilityVersion;
		FitValues(minValue, maxValue);
	}

	protected override void OnThink() {
		base.OnThink();
		RefreshCurves();
		DoAutoFrame();
	}

	// Painting

	private static double NiceStep(double raw) {
		double power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
		double fraction = raw / power;
		double nice = fraction switch {
			<= 1 => 1,
			<= 2 => 2,
			<= 5 => 5,
			_ => 10
		};
		return nice * power;
	}

	Hit? hovered;

	private void PaintCurves(float width, float height) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		Rows.ComputeMarks();
		if (animation == null) return;

		float endX = TimeToX(animation.CalculateMaxTime());
		if (endX < width) {
			Graphics2D.SetDrawColor(0, 0, 0, 70);
			Graphics2D.DrawRectangle(Math.Max(0, endX), 0, width - Math.Max(0, endX), height);
		}

		Graphics2D.SetDrawColor(255, 255, 255, 14);
		int majors = CalcXMajorDivisions();
		for (double frame = Math.Max(0, Math.Floor(XToFrameExact(0) / majors) * majors); ; frame += majors) {
			float x = (float)FrameToX(frame);
			if (x > width) break;
			Graphics2D.DrawLine(x, 0, x, height);
		}

		double step = NiceStep(70 / ValueZoom);
		int decimals = Math.Max(0, (int)-Math.Floor(Math.Log10(step)));
		for (double value = Math.Ceiling(YToValue(height) / step) * step; ; value += step) {
			float y = ValueToY(value);
			if (y < 0) break;
			bool zero = Math.Abs(value) < step / 2;
			Graphics2D.SetDrawColor(255, 255, 255, zero ? 40 : 14);
			Graphics2D.DrawLine(0, y, width, y);
			Graphics2D.SetDrawColor(150, 155, 162);
			Graphics2D.DrawText(width - 6, y, (zero ? 0 : value).ToString("F" + decimals),
				Graphics2D.UI_FONT_NAME, 18, Anchor.CenterRight);
		}

		var list = Curves;
		hovered = curvePanel.IsHovered() ? HitTest(curvePanel.GetMousePos()) : null;
		double end = animation.CalculateMaxTime();

		// Curves with a selected or hovered key go on top
		GraphCurve? top = hovered?.Curve;
		foreach (var curve in list) {
			if (curve != top && !HasSelection(curve))
				PaintCurve(curve, width, end);
		}
		foreach (var curve in list) {
			if (curve != top && HasSelection(curve))
				PaintCurve(curve, width, end);
		}
		if (top != null)
			PaintCurve(top, width, end);

		if (box is RectangleF boxBounds) {
			float x0 = (float)FrameToX(boxBounds.X), x1 = (float)FrameToX(boxBounds.X + boxBounds.W);
			float y0 = ValueToY(boxBounds.Y + boxBounds.H), y1 = ValueToY(boxBounds.Y);
			var rect = RectangleF.XYWH(Math.Min(x0, x1), y0, Math.Abs(x1 - x0), y1 - y0);
			Graphics2D.SetDrawColor(120, 170, 255, 40);
			Graphics2D.DrawRectangle(rect);
			Graphics2D.SetDrawColor(150, 190, 255);
			Graphics2D.DrawRectangleOutline(rect);
		}

		// White line while a drag is snapped to the key's frame or value
		if (mode == DragMode.Keys && pressHit is Hit hit && (snappedTime || snappedValue)) {
			Graphics2D.SetDrawColor(255, 255, 255, 160);
			if (snappedTime) {
				float x = TimeToX(hit.Key.Time);
				Graphics2D.DrawLine(x, 0, x, height);
			}
			if (snappedValue) {
				float y = ValueToY(hit.Key.Value);
				Graphics2D.DrawLine(0, y, width, y);
			}
		}
	}

	private static bool HasSelection(GraphCurve curve) {
		for (int i = 0; i < curve.Curve.Keyframes.Count; i++) {
			if (ModelEditor.Active.IsKeyframeSelected(curve.Curve.RefAt(i))) return true;
		}
		return false;
	}

	private void PaintCurve(GraphCurve curve, float width, double end) {
		var keys = curve.Curve.Keyframes;
		for (int i = 0; i < keys.Count; i++)
			curve.Curve.UpdateAutoHandles(i);

		var dim = new Color(curve.Color.R, curve.Color.G, curve.Color.B, (byte)70);
		if (Timeline.Repeat && end > 0) {
			Graphics2D.SetDrawColor(dim);
			PaintSegments(curve, -end, width);
			PaintSegments(curve, end, width);
			var first = keys[0];
			var last = keys[keys.Count - 1];
			Graphics2D.DrawLine(TimeToX(last.Time - end), ValueToY(last.Value), TimeToX(first.Time), ValueToY(first.Value));
			Graphics2D.DrawLine(TimeToX(last.Time), ValueToY(last.Value), TimeToX(first.Time + end), ValueToY(first.Value));
		}
		else {
			Graphics2D.SetDrawColor(dim);
			float firstY = ValueToY(keys[0].Value), lastY = ValueToY(keys[keys.Count - 1].Value);
			Graphics2D.DrawLine(0, firstY, TimeToX(keys[0].Time), firstY);
			Graphics2D.DrawLine(TimeToX(keys[keys.Count - 1].Time), lastY, width, lastY);
		}

		Graphics2D.SetDrawColor(curve.Color);
		PaintSegments(curve, 0, width);

		for (int i = 0; i < keys.Count; i++) {
			var key = keys[i];
			float x = TimeToX(key.Time);
			if (x < -20 || x > width + 20) continue;
			bool selected = ModelEditor.Active.IsKeyframeSelected(curve.Curve.RefAt(i));
			if (selected)
				PaintHandles(curve, i);

			float y = ValueToY(key.Value);
			var rect = RectangleF.XYWH(x - KEY_SIZE / 2, y - KEY_SIZE / 2, KEY_SIZE, KEY_SIZE);
			Graphics2D.SetDrawColor(selected ? new Color(255, 255, 255) : curve.Color);
			Graphics2D.DrawRectangle(rect);
			Graphics2D.SetDrawColor(selected ? curve.Color : new Color(10, 10, 10));
			Graphics2D.DrawRectangleOutline(rect);
		}
		foreach (var handle in selectedHandles) {
			int index = curve.Curve.IndexOf(handle.Key);
			if (index >= 0 && !ModelEditor.Active.IsKeyframeSelected(handle.Key))
				PaintHandles(curve, index);
		}
	}

	private void PaintSegments(GraphCurve curve, double offset, float width) {
		var keys = curve.Curve.Keyframes;
		for (int i = 0; i + 1 < keys.Count; i++) {
			Keyframe<float> key = keys[i];
			Keyframe<float> next = keys[i + 1];
			float x0 = TimeToX(key.Time + offset), x1 = TimeToX(next.Time + offset);
			if (x1 < 0) continue;
			if (x0 > width) break;
			float y0 = ValueToY(key.Value), y1 = ValueToY(next.Value);
			switch (key.Interpolation) {
				case KeyframeInterpolation.Constant:
					Graphics2D.DrawLine(x0, y0, x1, y0);
					Graphics2D.DrawLine(x1, y0, x1, y1);
					break;
				case KeyframeInterpolation.Linear:
					Graphics2D.DrawLine(x0, y0, x1, y1);
					break;
				default:
					int steps = Math.Clamp((int)((x1 - x0) / 5), 2, 48);
					float previousX = x0, previousY = y0;
					for (int step = 1; step <= steps; step++) {
						double time = key.Time + (next.Time - key.Time) * step / steps;
						float x = TimeToX(time + offset), y = ValueToY(Keyframe<float>.EvaluateBezier(time, key, next));
						Graphics2D.DrawLine(previousX, previousY, x, y);
						previousX = x;
						previousY = y;
					}
					break;
			}
		}
	}

	// Handle i is drawn when the segment on its side is bezier
	private static KeyframeHandle<float>? ShownHandle(List<Keyframe<float>> keys, int i, bool right) {
		if (right)
			return i + 1 < keys.Count && keys[i].Interpolation == KeyframeInterpolation.Bezier ? keys[i].RightHandle : null;
		return i > 0 && keys[i - 1].Interpolation == KeyframeInterpolation.Bezier ? keys[i].LeftHandle : null;
	}

	private void PaintHandles(GraphCurve curve, int i) {
		var keys = curve.Curve.Keyframes;
		var key = keys[i];
		var keyPos = new Vector2F(TimeToX(key.Time), ValueToY(key.Value));
		foreach (var right in new[] { false, true }) {
			if (ShownHandle(keys, i, right) is not KeyframeHandle<float> handle) continue;
			var pos = new Vector2F(TimeToX(handle.Time), ValueToY(handle.Value));
			bool selected = selectedHandles.Contains((curve.Curve.RefAt(i), right));
			Graphics2D.SetDrawColor(200, 200, 200, 170);
			Graphics2D.DrawDottedLine(keyPos, pos, 3);
			Graphics2D.SetDrawColor(selected ? new Color(255, 255, 255) : curve.Color);
			// automatic handles are triangles
			if (handle.HandleType is KeyframeHandleType.Automatic or KeyframeHandleType.AutoClamped or KeyframeHandleType.Vector) {
				Graphics2D.DrawTriangle(new(pos.X, pos.Y - HANDLE_RADIUS - 1), new(pos.X - HANDLE_RADIUS - 1, pos.Y + HANDLE_RADIUS), new(pos.X + HANDLE_RADIUS + 1, pos.Y + HANDLE_RADIUS));
			}
			else
				Graphics2D.DrawCircle(pos, HANDLE_RADIUS);
		}
	}

	protected override void PaintTimeOverlay(float width, float height) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null || Rows.Shown.Count == 0) return;
		DopesheetView.PaintRulerKeys(this, Rows.Shown[0].Marks, animation, width, height);
	}

	protected override void PaintPanelOverlay(float width, float height) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null) return;
		float endX = TimeToX(animation.CalculateMaxTime());
		Graphics2D.SetDrawColor(DopesheetView.END_COLOR);
		Graphics2D.DrawLine(endX, 0, endX, height);
	}

	// Curve area input

	enum DragMode { None, Pending, Keys, Handle, Box, Scale, Pan, ZoomAxes }

	DragMode mode;
	Vector2F pressPos;
	Vector2F lastMouse;
	bool pressCtrl;
	bool pressShift;
	bool pressAlt;
	bool rightPress;
	Hit? pressHit;
	double lastMoveTime;
	bool snappedTime, snappedValue;

	/// <summary>
	/// The box selection: X/W in frames, Y/H in values
	/// </summary>
	RectangleF? box;
	double boxStartFrame, boxStartValue;
	double scaleFixed, scaleEdge;

	KeyEdit? edit;
	List<KeyEdit.Key> moving = [];
	readonly List<(KeyEdit.Key Key, int Curve, float Value)> valueTargets = [];

	KeyframeHandle<float> handleStart;
	bool handleSeparate;

	double lastClickTime = -1;
	KeyframeRef<float>? lastClick;
	bool pressWasSelected;

	double panStartOffset, panStartValue;
	double zoomStartTime, zoomStartValue;
	double zoomStartX, zoomStartY;

	/// <summary>
	/// The key or handle under <paramref name="pos"/>, handles first
	/// </summary>
	public Hit? HitTest(Vector2F pos) {
		var list = Curves;
		for (int curveIndex = list.Count - 1; curveIndex >= 0; curveIndex--) {
			var curve = list[curveIndex];
			var keys = curve.Curve.Keyframes;
			for (int i = 0; i < keys.Count; i++) {
				var key = curve.Curve.RefAt(i);
				if (!ModelEditor.Active.IsKeyframeSelected(key) && !selectedHandles.Contains((key, false)) && !selectedHandles.Contains((key, true))) continue;
				foreach (var right in new[] { false, true }) {
					if (ShownHandle(keys, i, right) is KeyframeHandle<float> handle && Near(pos, handle.Time, handle.Value))
						return new Hit(curve, key, right ? 1 : -1);
				}
			}
		}
		var stack = KeysAt(pos);
		if (stack.Count == 0) return null;
		// keys on top of each other: pick the selected one
		List<Hit> selected = [];
		foreach (var stacked in stack) {
			if (ModelEditor.Active.IsKeyframeSelected(stacked.Key))
				selected.Add(stacked);
		}
		return selected.Count == 1 ? selected[0] : stack[0];
	}

	/// <summary>
	/// Every key under <paramref name="pos"/>, topmost curve first
	/// </summary>
	public List<Hit> KeysAt(Vector2F pos) {
		var list = Curves;
		List<Hit> stack = [];
		for (int curveIndex = list.Count - 1; curveIndex >= 0; curveIndex--) {
			var curve = list[curveIndex].Curve;
			for (int i = 0; i < curve.Keyframes.Count; i++) {
				var key = curve.RefAt(i);
				if (Near(pos, key.Time, key.Value) && !stack.Any(h => h.Key == key))
					stack.Add(new Hit(list[curveIndex], key, 0));
			}
		}
		return stack;
	}

	private bool Near(Vector2F pos, double time, double value) => Math.Abs(TimeToX(time) - pos.X) <= HIT && Math.Abs(ValueToY(value) - pos.Y) <= HIT;

	private bool OnBoxEdge(Vector2F pos, out bool right) {
		right = false;
		if (box is not RectangleF boxBounds) return false;
		if (pos.Y < ValueToY(boxBounds.Y + boxBounds.H) || pos.Y > ValueToY(boxBounds.Y)) return false;
		float x0 = (float)FrameToX(boxBounds.X), x1 = (float)FrameToX(boxBounds.X + boxBounds.W);
		if (Math.Abs(pos.X - x1) <= HIT) {
			right = true;
			return true;
		}
		return Math.Abs(pos.X - x0) <= HIT;
	}

	private double FrameAt(float x, bool snap) => snap ? XToFrame(x) : XToFrameExact(x);

	private void Focus() {
		Focused = true;
		ModelEditor.Active.Dopesheet.Focused = false;
	}

	private bool CurvePress(ButtonCode button, Vector2F pos, FrameState state) {
		Focus();
		pressPos = lastMouse = pos;
		lastMoveTime = EngineCore.Level.Realtime;
		pressCtrl = state.Keyboard.ControlDown;
		pressShift = state.Keyboard.ShiftDown;
		pressAlt = state.Keyboard.AltDown;
		pressHit = HitTest(pos);
		pressWasSelected = pressHit is Hit pressed && pressed.Side == 0 && ModelEditor.Active.IsKeyframeSelected(pressed.Key);
		rightPress = button == ButtonCode.Mouse2;

		if (rightPress) {
			mode = DragMode.Pending;
			return true;
		}
		if (button != ButtonCode.Mouse1) return true;
		mode = DragMode.Pending;

		if (OnBoxEdge(pos, out bool right) && ModelEditor.Active.KeyframesSelected) {
			var boxBounds = box!.Value;
			scaleFixed = right ? boxBounds.X : boxBounds.X + boxBounds.W;
			scaleEdge = right ? boxBounds.X + boxBounds.W : boxBounds.X;
			mode = DragMode.Scale;
			edit = null;
			return true;
		}

		if (pressHit is Hit hit && hit.Side == 0 && (!pressCtrl || pressShift) && !ModelEditor.Active.IsKeyframeSelected(hit.Key)) {
			ModelEditor.Active.SelectKeyframes([hit.Key]);
			selectedHandles.Clear();
		}
		return true;
	}

	private bool CurveDrag(Vector2F pos, FrameState state) {
		if (pos != lastMouse) {
			lastMouse = pos;
			lastMoveTime = EngineCore.Level.Realtime;
		}
		bool snap = !state.Keyboard.ShiftDown;

		switch (mode) {
			case DragMode.Pending:
				if ((pos - pressPos).Length < DRAG_START) return true;
				if (rightPress) {
					if (pressAlt) {
						mode = DragMode.ZoomAxes;
						zoomStartTime = Zoom;
						zoomStartValue = ValueZoom;
						zoomStartX = XToFrameExact(pressPos.X);
						zoomStartY = YToValue(pressPos.Y);
					}
					else {
						mode = DragMode.Pan;
						panStartOffset = FrameOffset;
						panStartValue = ValueCenter;
					}
				}
				else if (pressHit is Hit hit && hit.Side != 0)
					BeginHandleDrag(hit);
				else if (pressHit != null && (!pressCtrl || pressShift))
					BeginKeyDrag(pressCtrl && pressShift);
				else {
					mode = DragMode.Box;
					boxStartFrame = FrameAt(pressPos.X, snap);
					boxStartValue = YToValue(pressPos.Y);
				}
				return CurveDrag(pos, state);
			case DragMode.Pan:
				FrameOffset = panStartOffset - (pos.X - pressPos.X);
				ValueCenter = panStartValue + (pos.Y - pressPos.Y) / ValueZoom;
				return true;
			case DragMode.ZoomAxes:
				var delta = pos - pressPos;
				Zoom = zoomStartTime * Math.Exp(delta.X * 0.01);
				FrameOffset = defaultXOffset + zoomStartX * Zoom - pressPos.X;
				ValueZoom = zoomStartValue * Math.Exp(-delta.Y * 0.01);
				ValueCenter = zoomStartY - (curveHeight / 2 - pressPos.Y) / ValueZoom;
				return true;
			case DragMode.Keys:
				UpdateKeyDrag(pos, snap);
				return true;
			case DragMode.Handle:
				UpdateHandleDrag(pos, state.Keyboard.AltDown);
				return true;
			case DragMode.Box:
				double frame = FrameAt(pos.X, snap);
				double value = YToValue(pos.Y);
				box = RectangleF.XYWH(
					(float)Math.Min(boxStartFrame, frame),
					(float)Math.Min(boxStartValue, value),
					(float)Math.Abs(frame - boxStartFrame),
					(float)Math.Abs(value - boxStartValue));
				return true;
			case DragMode.Scale:
				if (edit == null) {
					if ((pos - pressPos).Length < 1) return true;
					BeginScale();
				}
				UpdateScale(FrameAt(pos.X, snap), snap);
				return true;
		}
		return true;
	}

	private bool CurveRelease(ButtonCode button, Vector2F pos, FrameState state) {
		var was = mode;
		mode = DragMode.None;
		switch (was) {
			case DragMode.Keys:
			case DragMode.Scale:
				edit?.ResolveOverlaps(moving);
				edit = null;
				moving = [];
				valueTargets.Clear();
				snappedTime = snappedValue = false;
				Rows.MarkDirty();
				break;
			case DragMode.Box:
				SelectInBox(pressCtrl);
				if (EngineCore.Level.Realtime - lastMoveTime < BOX_KEEP_PAUSE)
					box = null;
				break;
			case DragMode.Pending:
				if (rightPress)
					RightClick();
				else
					Click();
				break;
		}
		return true;
	}

	// right clicking a key selects its item
	private void RightClick() {
		if (pressHit is Hit hit && hit.Curve.Row.Owner is IEditorType owner)
			ModelEditor.Active.SelectObject(owner);
	}

	private void Click() {
		var now = EngineCore.Level.Realtime;
		if (pressHit is not Hit hit) {
			lastClick = null;
			box = null;
			if (!pressCtrl) {
				ModelEditor.Active.UnselectAllKeyframes();
				selectedHandles.Clear();
			}
			return;
		}

		if (hit.Side != 0) {
			var handle = (hit.Key, hit.Side > 0);
			if (pressCtrl) {
				if (!selectedHandles.Remove(handle))
					selectedHandles.Add(handle);
			}
			else {
				selectedHandles.Clear();
				selectedHandles.Add(handle);
			}
			return;
		}

		// Clicking a selected key that has others under it selects the next one down.
		if (!pressCtrl && pressWasSelected && KeysAt(pressPos) is { Count: > 1 } stack) {
			int index = stack.FindIndex(h => h.Key == hit.Key);
			hit = stack[(index + 1) % stack.Count];
			lastClick = null;
			ModelEditor.Active.SelectKeyframes([hit.Key]);
			selectedHandles.Clear();
			Timeline.SetFrame(hit.Key.Time * FPS);
			return;
		}

		bool doubleClick = lastClick == hit.Key && now - lastClickTime < DOUBLE_CLICK;
		lastClick = doubleClick ? null : hit.Key;
		lastClickTime = now;
		if (doubleClick) {
			ModelEditor.Active.SelectKeyframes([hit.Key]);
			Rows.DeleteSelected();
			selectedHandles.Clear();
			return;
		}

		if (pressCtrl) {
			if (ModelEditor.Active.IsKeyframeSelected(hit.Key))
				ModelEditor.Active.UnselectKeyframe(hit.Key);
			else
				ModelEditor.Active.SelectKeyframes([hit.Key], true);
		}
		else {
			ModelEditor.Active.SelectKeyframes([hit.Key]);
			selectedHandles.Clear();
			Timeline.SetFrame(hit.Key.Time * FPS);
		}
		if (Rows.Locked && hit.Curve.Row.Owner is IEditorType owner)
			ModelEditor.Active.SelectObject(owner);
	}

	private void SelectInBox(bool additive) {
		if (box is not RectangleF boxBounds) return;
		List<IKeyframe> keys = [];
		foreach (var curve in Curves) {
			for (int i = 0; i < curve.Curve.Keyframes.Count; i++) {
				var key = curve.Curve.Keyframes[i];
				double frame = key.Time * FPS;
				if (frame >= boxBounds.X - KeyEdit.TIME_EPSILON && frame <= boxBounds.X + boxBounds.W + KeyEdit.TIME_EPSILON && key.Value >= boxBounds.Y && key.Value <= boxBounds.Y + boxBounds.H)
					keys.Add(curve.Curve.RefAt(i));
			}
		}
		if (!additive)
			selectedHandles.Clear();
		ModelEditor.Active.SelectKeyframes(keys, additive);
	}

	private void BeginKeyDrag(bool duplicate) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null || pressHit is not Hit hit) {
			mode = DragMode.None;
			return;
		}
		mode = DragMode.Keys;
		edit = new KeyEdit(animation, Rows.SelectedTimelines());
		List<KeyEdit.Key> originals = [];
		foreach (var original in edit.Keys) {
			if (AnimationRows.IsSelected(original))
				originals.Add(original);
		}
		List<int[]> targets = [];
		foreach (var original in originals) {
			List<int> indices = [];
			for (int i = 0; i < original.Curves.Length; i++) {
				if (visibleCurves.Contains(original.Curves[i]) && ModelEditor.Active.IsKeyframeSelected(original.Keyframes[i]))
					indices.Add(i);
			}
			targets.Add(indices.ToArray());
		}
		if (duplicate) {
			List<KeyEdit.Key> copies = [];
			foreach (var original in originals)
				copies.Add(edit.Duplicate(original));
			moving = copies;
		}
		else
			moving = originals;

		valueTargets.Clear();
		List<IKeyframe> selection = [];
		for (int index = 0; index < moving.Count; index++) {
			var key = moving[index];
			foreach (int i in targets[index]) {
				selection.Add(key.Keyframes[i]);
				if (key.Timeline is not DeformTimeline && key.Keyframes[i] is KeyframeRef<float> floatKey)
					valueTargets.Add((key, i, floatKey.Value));
				if (originals[index].Keyframes[i] == hit.Key)
					pressHit = hit with { Key = (KeyframeRef<float>)key.Keyframes[i] };
			}
		}
		if (duplicate)
			ModelEditor.Active.SelectKeyframes(selection);
	}

	private void UpdateKeyDrag(Vector2F pos, bool snap) {
		if (edit == null || pressHit is not Hit hit) return;
		var delta = pos - pressPos;
		// Vertical drags lock the frame, horizontal ones lock the value
		snappedTime = Math.Abs(delta.X) < AXIS_SNAP;
		snappedValue = Math.Abs(delta.Y) < AXIS_SNAP;

		var press = moving.FirstOrDefault(k => k.Keyframes.Contains(hit.Key));
		double start = (press?.OriginalTime ?? hit.Key.Time) * FPS;
		double target = start + XToFrameExact(pos.X) - XToFrameExact(pressPos.X);
		if (snap)
			target = Math.Round(target);
		double dt = snappedTime ? 0 : (target - start) / FPS;
		double valueDelta = snappedValue ? 0 : -delta.Y / ValueZoom;

		foreach (var key in moving)
			edit.SetTime(key, key.OriginalTime + dt);
		foreach (var valueTarget in valueTargets)
			edit.SetValue(valueTarget.Key, valueTarget.Curve, (float)(valueTarget.Value + valueDelta));
		edit.Finish();
	}

	private void BeginScale() {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null) {
			mode = DragMode.None;
			return;
		}
		edit = new KeyEdit(animation, Rows.SelectedTimelines());
		List<KeyEdit.Key> selected = [];
		foreach (var key in edit.Keys) {
			if (AnimationRows.IsSelected(key))
				selected.Add(key);
		}
		moving = selected;
	}

	private void UpdateScale(double edgeFrame, bool snap) {
		if (edit == null || box is not RectangleF boxBounds) return;
		double span = scaleEdge - scaleFixed;
		if (Math.Abs(span) < 1e-9) return;
		double scale = (edgeFrame - scaleFixed) / span;
		foreach (var key in moving) {
			double frame = scaleFixed + (key.OriginalTime * FPS - scaleFixed) * scale;
			edit.SetTime(key, (snap ? Math.Round(frame) : frame) / FPS);
		}
		edit.Finish();
		double low = Math.Min(scaleFixed, edgeFrame), high = Math.Max(scaleFixed, edgeFrame);
		box = RectangleF.XYWH((float)low, boxBounds.Y, (float)(high - low), boxBounds.H);
	}

	private void BeginHandleDrag(Hit hit) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		var handle = hit.Side > 0 ? hit.Key.RightHandle : hit.Key.LeftHandle;
		if (animation == null || handle == null) {
			mode = DragMode.None;
			return;
		}
		mode = DragMode.Handle;
		ModelEditor.Active.Actions.TrackAnimation(animation);
		handleStart = handle.Value;
		handleSeparate = handle.Value.HandleType == KeyframeHandleType.Free;
		var selectedHandle = (hit.Key, hit.Side > 0);
		if (!selectedHandles.Contains(selectedHandle)) {
			selectedHandles.Clear();
			selectedHandles.Add(selectedHandle);
		}
	}

	// Joined handles stay in line, alt toggles separate
	// moved automatic handles become manual
	private void UpdateHandleDrag(Vector2F pos, bool alt) {
		if (pressHit is not Hit hit) return;
		var key = hit.Key;
		bool right = hit.Side > 0;
		bool separate = handleSeparate ^ alt;

		double time = handleStart.Time + (XToFrameExact(pos.X) - XToFrameExact(pressPos.X)) / FPS;
		double value = handleStart.Value - (pos.Y - pressPos.Y) / ValueZoom;
		time = right ? Math.Max(key.Time, time) : Math.Min(key.Time, time);
		var type = separate ? KeyframeHandleType.Free : KeyframeHandleType.Aligned;
		var moved = new KeyframeHandle<float> { HandleType = type, Time = time, Value = (float)value };

		var other = right ? key.LeftHandle : key.RightHandle;
		if (other is KeyframeHandle<float> opposite) {
			if (!separate) {
				// in pixels
				double handleX = (time - key.Time) * FPS * Zoom, handleY = (value - key.Value) * ValueZoom;
				double length = Math.Sqrt(handleX * handleX + handleY * handleY);
				double oppositeX = (opposite.Time - key.Time) * FPS * Zoom;
				double oppositeY = (opposite.Value - key.Value) * ValueZoom;
				double otherLength = Math.Sqrt(oppositeX * oppositeX + oppositeY * oppositeY);
				if (length > 1e-6) {
					oppositeX = -handleX / length * otherLength;
					oppositeY = -handleY / length * otherLength;
					opposite = opposite with {
						Time = key.Time + oppositeX / (FPS * Zoom),
						Value = (float)(key.Value + oppositeY / ValueZoom)
					};
				}
				opposite.HandleType = KeyframeHandleType.Aligned;
			}
			else
				opposite.HandleType = KeyframeHandleType.Free;
			if (right)
				key.LeftHandle = opposite;
			else
				key.RightHandle = opposite;
		}
		if (right)
			key.RightHandle = moved;
		else
			key.LeftHandle = moved;

		var animation = ModelEditor.Active.File.ActiveAnimation!;
		ModelEditor.Active.File.NotifyAnimationEdited(animation);
		ModelEditor.Active.QueueAnimationUpdate();
	}

	/// <summary>
	/// Selects every key on the curves of the selected keys, or every visible key after that.
	/// </summary>
	public void SelectAllKeys() {
		List<IKeyframe> withSelection = [];
		foreach (var curve in Curves) {
			if (!HasSelection(curve)) continue;
			for (int i = 0; i < curve.Curve.Keyframes.Count; i++)
				withSelection.Add(curve.Curve.RefAt(i));
		}
		if (withSelection.Count > 0 && !withSelection.All(ModelEditor.Active.IsKeyframeSelected))
			ModelEditor.Active.SelectKeyframes(withSelection);
		else {
			List<IKeyframe> all = [];
			foreach (var curve in Curves) {
				for (int i = 0; i < curve.Curve.Keyframes.Count; i++)
					all.Add(curve.Curve.RefAt(i));
			}
			ModelEditor.Active.SelectKeyframes(all);
		}
	}

	class CurvePanel(GraphView view) : Panel(view)
	{
		public override void Paint(float width, float height) => view.PaintCurves(width, height);
		protected override bool MouseClick(FrameState state, ButtonCode button) => view.CurvePress(button, GetMousePos(), state);
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) => view.CurveDrag(GetMousePos(), state);
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) => view.CurveRelease(button, GetMousePos(), state);
		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			var pos = GetMousePos();
			double factor = Math.Pow(1.15, delta.Y);
			double value = view.YToValue(pos.Y);
			view.ZoomAround(view.Zoom * factor, pos.X);
			view.ValueZoom *= factor;
			view.ValueCenter = value - (view.curveHeight / 2 - pos.Y) / view.ValueZoom;
			return true;
		}
	}

	// Names column

	int dropIndex = -1;
	TimelineRow? namePressRow;
	Vector2F namePressPos;
	bool nameDragging;

	private TimelineRow? RowAt(float y) {
		var shown = Rows.Shown;
		int index = (int)Math.Floor((y + ScrollOffset) / ROW_HEIGHT);
		return index >= 0 && index < shown.Count ? shown[index] : null;
	}

	private void PaintNames(float width, float height) {
		RefreshCurves();
		DopesheetView.PaintRowNames(Rows, ScrollOffset, width - DOT_SPACE, height, 0);
		var shown = Rows.Shown;
		for (int i = Math.Max(0, (int)(ScrollOffset / ROW_HEIGHT)); i < shown.Count; i++) {
			float y = i * ROW_HEIGHT - ScrollOffset;
			if (y > height) break;
			var row = shown[i];
			Graphics2D.SetDrawColor(DopesheetView.RowColor(row));
			Graphics2D.DrawRectangle(width - DOT_SPACE, y, DOT_SPACE, ROW_HEIGHT);
			Graphics2D.SetDrawColor(0, 0, 0, 120);
			Graphics2D.DrawLine(width - DOT_SPACE, y + ROW_HEIGHT - 1, width, y + ROW_HEIGHT - 1);
			if (row.Kind == RowKind.Property && row.Timeline is DeformTimeline or EventTimeline or DrawOrderTimeline) continue;
			if (row.Kind == RowKind.Property && !row.Curves.Any(c => c is FCurve<float>)) continue;

			var center = new Vector2F(width - DOT_SPACE / 2, y + ROW_HEIGHT / 2);
			int shownCount = 0;
			int total = 0;
			foreach (var property in Rows.PropertiesUnder(row)) {
				total++;
				if (visible.Contains(property.Timeline!))
					shownCount++;
			}
			Graphics2D.SetDrawColor(shownCount == 0 ? new Color(120, 125, 132) : new Color(235, 235, 235));
			if (shownCount == 0)
				Graphics2D.DrawRing(center, 4, 5.5f);
			else if (shownCount < total) {
				Graphics2D.DrawRing(center, 4, 5.5f);
				Graphics2D.DrawCircle(center, 2.5f);
			}
			else
				Graphics2D.DrawCircle(center, 5.5f);
		}
		if (dropIndex >= 0) {
			Graphics2D.SetDrawColor(FrameMarkerColor);
			Graphics2D.DrawRectangle(0, dropIndex * ROW_HEIGHT - ScrollOffset - 1, width, 2);
		}
	}

	private bool NamesPress(ButtonCode button, Vector2F pos, FrameState state) {
		Focus();
		namePressRow = RowAt(pos.Y);
		namePressPos = pos;
		nameDragging = false;
		if (namePressRow == null) return true;

		if (pos.X >= names.GetRenderBounds().W - DOT_SPACE) {
			ClickDot(namePressRow, button == ButtonCode.Mouse2 || state.Keyboard.ControlDown);
			namePressRow = null;
			return true;
		}
		if (namePressRow.Kind == RowKind.Group && (button == ButtonCode.Mouse2 || pos.X < 20)) {
			Rows.ToggleCollapsed(namePressRow.Owner!);
			namePressRow = null;
		}
		return true;
	}

	private bool NamesDrag(Vector2F pos) {
		if (namePressRow?.Kind != RowKind.Group) return true;
		if (!nameDragging && Math.Abs(pos.Y - namePressPos.Y) < DRAG_START) return true;
		nameDragging = true;
		dropIndex = DopesheetView.GroupDropIndex(Rows, pos.Y + ScrollOffset);
		return true;
	}

	private bool NamesRelease(ButtonCode button, Vector2F pos, FrameState state) {
		var row = namePressRow;
		namePressRow = null;
		if (nameDragging && row != null) {
			DopesheetView.DropGroup(Rows, row, dropIndex);
			dropIndex = -1;
			nameDragging = false;
			return true;
		}
		if (row != null && button == ButtonCode.Mouse1)
			DopesheetView.ClickRowName(row, state.Keyboard.ControlDown);
		return true;
	}

	class NamesPanel(GraphView view, Element parent) : Panel(parent)
	{
		public override void Paint(float width, float height) => view.PaintNames(width, height);
		protected override bool MouseClick(FrameState state, ButtonCode button) => view.NamesPress(button, GetMousePos(), state);
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) => view.NamesDrag(GetMousePos());
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) => view.NamesRelease(button, GetMousePos(), state);
		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			view.ScrollOffset -= delta.Y * ROW_HEIGHT * 2;
			return true;
		}
	}
}
