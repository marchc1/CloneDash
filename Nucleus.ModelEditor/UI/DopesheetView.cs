using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI;

public class DopesheetView : BaseTimelineView
{
	public override string Name => "Dope Sheet";
	public override bool LockDragDirection => false;

	public const float ROW_HEIGHT = 26;
	private const float KEY_WIDTH = 7;
	private const float KEY_HIT = 6;
	private const float DRAG_START = 4;
	private const double DOUBLE_CLICK = 0.3;
	// hold a box drag still this long before release and the box is left on screen
	private const double BOX_KEEP_PAUSE = 0.25;

	public static readonly Color END_COLOR = new(255, 140, 30);

	public AnimationRows Rows { get; } = new();

	/// <summary>
	/// Shift mode: moving a key also moves every key after it
	/// </summary>
	public bool ShiftKeys { get; set; }
	public bool FollowPlayhead { get; set; }
	/// <summary>
	/// Set by clicks in the dopesheet
	/// </summary>
	public bool Focused { get; set; }

	public IReadOnlyList<TimelineRow> ShownRows => Rows.Shown;
	public IReadOnlyList<TimelineRow> AllRows => Rows.All;
	public bool Locked => Rows.Locked;

	static TimelineManager Timeline => ModelEditor.Active.File.Timeline;
	static double FPS => Timeline.GetReferenceFPS();

	readonly NamesPanel names;
	readonly KeysPanel keysPanel;
	Textbox currentBox = null!;
	Textbox loopStartBox = null!;
	Textbox loopEndBox = null!;

	public DopesheetView(Element parent) : base(parent) {
		keysPanel = new(this);
		keysPanel.Dock = Dock.Fill;
		keysPanel.DockMargin = RectangleF.Zero;
		keysPanel.SetPaintBackgroundEnabled(false);
		keysPanel.SetPaintBorderEnabled(false);

		names = new(this, ButtonsAndNames);
		names.Dock = Dock.Fill;
		names.DockMargin = RectangleF.Zero;
		names.SetPaintBackgroundEnabled(false);
		names.SetPaintBorderEnabled(false);

		BuildToolbar();
		KeyframeOverlay.MoveToFront();

		ModelEditor.Active.File.KeyframeCreated += (_, _, _) => Rows.MarkDirty();
		ModelEditor.Active.File.KeyframeRemoved += (_, _, _) => Rows.MarkDirty();
		ModelEditor.Active.File.Timeline.FrameChanged += (_, _) => SyncCurrentBox();
		ModelEditor.Active.File.Timeline.FrameElapsed += (_, _) => SyncCurrentBox();
	}

	public override void CreateChannels() {
		Rows.MarkDirty();
		// An edit in between ends a double click.
		lastClick = null;
	}

	protected override float MaxScrollOffset => Math.Max(0, Rows.Shown.Count * ROW_HEIGHT - RowsHeight + ROW_HEIGHT);
	public float RowsHeight => keysPanel?.GetRenderBounds().H ?? 0;

	public void SetLocked(bool locked) => Rows.SetLocked(locked);
	public void RefreshLocked() => Rows.RefreshLocked();
	public void SelectLockedBones() => Rows.SelectLockedBones();
	public void ToggleCollapsed(IEditorType owner) => Rows.ToggleCollapsed(owner);
	public bool IsCollapsed(IEditorType owner) => Rows.IsCollapsed(owner);
	public IEnumerable<IKeyframe> KeysAt(TimelineRow row, double time) => Rows.KeysAt(row, time);
	public void DeleteSelected() => Rows.DeleteSelected();
	public void CopySelected() => Rows.CopySelected();
	public void CutSelected() => Rows.CutSelected();
	public void Paste() => Rows.Paste();
	public void SelectAllKeys() => Rows.SelectAllKeys();
	public void SetInterpolation(KeyframeInterpolation interpolation) => Rows.SetInterpolation(interpolation);
	public KeyEdit.Clip? Clipboard => AnimationRows.Clipboard;

	// Toolbar

	internal static Panel ToolbarRow(Panel parent, float top) {
		var row = new Panel(parent);
		row.Dock = Dock.Top;
		row.Size = new(ToolRows.ROW_HEIGHT);
		row.DockMargin = RectangleF.TLRB(top, 0, 0, 0);
		row.DockPadding = RectangleF.Zero;
		row.SetPaintBackgroundEnabled(false);
		row.SetPaintBorderEnabled(false);
		return row;
	}

	internal static Button TextButton(Panel row, string text, float width, Action click) {
		var button = new Button(row);
		button.Dock = Dock.Left;
		button.Size = new(width);
		button.DockMargin = RectangleF.TLRB(0, 0, ToolRows.GAP, 0);
		button.Text = text;
		button.TextSize = 18;
		button.OnButtonClick += (_, _) => click();
		return button;
	}

	internal static CheckboxButton ToggleButton(Panel row, string text, float width, Func<bool> get, Action<bool> set) {
		var button = new CheckboxButton(row);
		button.Dock = Dock.Left;
		button.Size = new(width);
		button.DockMargin = RectangleF.TLRB(0, 0, ToolRows.GAP, 0);
		button.Text = text;
		button.TextSize = 18;
		button.OnCheckedChanged += (c) => set(c.Checked);
		button.Thinking += (_) => button.Checked = get();
		return button;
	}

	internal static Button IconButton(Panel row, string icon, Action click) {
		var button = new Button(row);
		button.Dock = Dock.Left;
		button.Size = new(ToolRows.ROW_HEIGHT);
		button.DockMargin = RectangleF.TLRB(0, 0, ToolRows.GAP, 0);
		button.Text = "";
		var image = SetButtonImage(button, textures.LoadTextureFromFile(icon));
		image.ImageOrientation = ImageOrientation.Zoom;
		image.ImagePadding = new(4);
		button.OnButtonClick += (_, _) => click();
		return button;
	}

	internal static void Space(Panel row, float width) {
		var gap = new Panel(row);
		gap.Dock = Dock.Left;
		gap.Size = new(width);
		gap.SetVisible(false);
	}

	private static Textbox FrameBox(Panel row, float width) {
		var box = new Textbox(row);
		box.Dock = Dock.Left;
		box.Size = new(width);
		box.DockMargin = RectangleF.TLRB(0, 0, ToolRows.GAP * 3, 0);
		box.TextSize = 18;
		return box;
	}

	/// <summary>
	/// Lock, Refresh and Select, shared with the graph
	/// </summary>
	internal static void LockButtons(Panel row, AnimationRows rows) {
		ToggleButton(row, "Lock", 64, () => rows.Locked, rows.SetLocked);
		var refresh = TextButton(row, "Refresh", 84, rows.RefreshLocked);
		var select = TextButton(row, "Select", 70, rows.SelectLockedBones);
		refresh.Thinking += (_) => refresh.SetMouseInputEnabled(rows.Locked);
		select.Thinking += (_) => select.SetMouseInputEnabled(rows.Locked);
	}

	/// <summary>
	/// Copy, cut, delete, paste, then stepped, linear and bezier
	/// </summary>
	internal static void KeyButtons(Panel row, AnimationRows rows) {
		var copy = IconButton(row, "models/copy.png", rows.CopySelected);
		var cut = IconButton(row, "models/cut.png", rows.CutSelected);
		var delete = IconButton(row, "models/remove.png", rows.DeleteSelected);
		var paste = IconButton(row, "models/paste.png", rows.Paste);
		foreach (var button in new[] { copy, cut, delete })
			button.Thinking += (s) => s.SetMouseInputEnabled(ModelEditor.Active.KeyframesSelected);
		paste.Thinking += (s) => s.SetMouseInputEnabled(AnimationRows.Clipboard != null && ModelEditor.Active.File.ActiveAnimation != null);
		Space(row, 12);

		var stepped = IconButton(row, "models/curve_constant.png",
			() => rows.SetInterpolation(KeyframeInterpolation.Constant));
		var linear = IconButton(row, "models/curve_linear.png",
			() => rows.SetInterpolation(KeyframeInterpolation.Linear));
		var bezier = IconButton(row, "models/curve_bezier.png",
			() => rows.SetInterpolation(KeyframeInterpolation.Bezier));
		foreach (var button in new[] { stepped, linear, bezier })
			button.Thinking += (s) => s.SetMouseInputEnabled(ModelEditor.Active.KeyframesSelected);
	}

	private void BuildToolbar() {
		// size counts the 6px margins
		TopButtonPanel.Size = new(0, ToolRows.ROW_HEIGHT * 2 + ToolRows.GAP + 12);

		var top = ToolbarRow(TopButtonPanel, 0);
		ToggleButton(top, "Current", 90, () => FollowPlayhead, v => FollowPlayhead = v);
		currentBox = FrameBox(top, 70);
		currentBox.OnUserPressedEnter += (_, _, text) => {
			if (double.TryParse(text, out var frame))
				Timeline.SetFrame(Math.Max(0, frame));
			SyncCurrentBox();
		};
		SyncCurrentBox();

		TextButton(top, "Loop Start", 100, () => SetLoop(true));
		loopStartBox = FrameBox(top, 60);
		loopStartBox.OnUserPressedEnter += (_, _, text) => Timeline.LoopStart = double.TryParse(text, out var frame) ? Math.Max(0, frame) : null;
		TextButton(top, "End", 56, () => SetLoop(false));
		loopEndBox = FrameBox(top, 60);
		loopEndBox.OnUserPressedEnter += (_, _, text) => Timeline.LoopEnd = double.TryParse(text, out var frame) ? Math.Max(0, frame) : null;
		loopStartBox.Thinking += (_) => {
			if (!loopStartBox.IsKeyboardFocused())
				loopStartBox.Text = Timeline.LoopStart?.ToString("0.##") ?? "";
		};
		loopEndBox.Thinking += (_) => {
			if (!loopEndBox.IsKeyboardFocused())
				loopEndBox.Text = Timeline.LoopEnd?.ToString("0.##") ?? "";
		};

		var bottom = ToolbarRow(TopButtonPanel, ToolRows.GAP);
		LockButtons(bottom, Rows);
		Space(bottom, 12);
		KeyButtons(bottom, Rows);
		Space(bottom, 12);
		ToggleButton(bottom, "Shift", 70, () => ShiftKeys, v => ShiftKeys = v);
	}

	private void SyncCurrentBox() {
		if (currentBox == null || currentBox.IsKeyboardFocused()) return;
		currentBox.Text = $"{Math.Round(Timeline.GetVisualPlayhead(false), 2)}";
	}

	// Set from the timeline position, clicking again clears it
	private void SetLoop(bool start) {
		double frame = Math.Round(Timeline.Frame * FPS, 2);
		if (start)
			Timeline.LoopStart = Timeline.LoopStart == frame ? null : frame;
		else
			Timeline.LoopEnd = Timeline.LoopEnd == frame ? null : frame;
	}

	// Painting

	internal static Color RowColor(TimelineRow row) {
		bool selected = row.Kind switch {
			RowKind.Group => ModelEditor.Active.IsObjectSelected(row.Owner),
			RowKind.Property => row.Parent?.Kind == RowKind.Group && ModelEditor.Active.IsObjectSelected(row.Parent.Owner),
			_ => false
		};
		return row.Kind switch {
			RowKind.Overview => new(58, 82, 92),
			RowKind.Group => selected ? new(70, 100, 112) : new(46, 64, 74),
			_ => selected ? new(40, 49, 60) : new(30, 36, 44)
		};
	}

	int firstVisibleRow => Math.Max(0, (int)(ScrollOffset / ROW_HEIGHT));

	/// <summary>
	/// Where a key on <paramref name="row"/> is drawn, in screen space
	/// </summary>
	public Vector2F KeyScreenPosition(TimelineRow row, double time) => keysPanel.GetGlobalPosition()
			+ new Vector2F((float)FrameToX(time * FPS), Rows.IndexOf(row) * ROW_HEIGHT - ScrollOffset + ROW_HEIGHT / 2);

	public Vector2F NameScreenPosition(TimelineRow row, float x)
		=> names.GetGlobalPosition() + new Vector2F(x, Rows.IndexOf(row) * ROW_HEIGHT - ScrollOffset + ROW_HEIGHT / 2);

	private void PaintKeys(float width, float height) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		var shown = Rows.Shown;
		Rows.ComputeMarks();
		if (animation == null) return;

		float endX = (float)FrameToX(animation.CalculateMaxTime() * FPS);
		int majors = CalcXMajorDivisions();
		for (int i = firstVisibleRow; i < shown.Count; i++) {
			float y = i * ROW_HEIGHT - ScrollOffset;
			if (y > height) break;
			var row = shown[i];

			Graphics2D.SetDrawColor(RowColor(row));
			Graphics2D.DrawRectangle(0, y, width, ROW_HEIGHT);
			if (endX < width) {
				Graphics2D.SetDrawColor(0, 0, 0, 80);
				Graphics2D.DrawRectangle(Math.Max(0, endX), y, width - Math.Max(0, endX), ROW_HEIGHT);
			}

			Graphics2D.SetDrawColor(0, 0, 0, 50);
			for (double frame = Math.Max(0, Math.Floor(XToFrameExact(0) / majors) * majors); ; frame += majors) {
				float x = (float)FrameToX(frame);
				if (x > width) break;
				Graphics2D.DrawLine(x, y, x, y + ROW_HEIGHT);
			}
			Graphics2D.SetDrawColor(0, 0, 0, 120);
			Graphics2D.DrawLine(0, y + ROW_HEIGHT - 1, width, y + ROW_HEIGHT - 1);

			if (row.Kind == RowKind.Property)
				PaintLines(row, y, width);
			foreach (var mark in row.Marks)
				PaintMark(mark, y, width);
		}

		if (box is RectangleF area) {
			float x0 = (float)FrameToX(area.X), x1 = (float)FrameToX(area.X + area.W);
			var rect = RectangleF.XYWH(Math.Min(x0, x1), area.Y - ScrollOffset, Math.Abs(x1 - x0), area.H);
			Graphics2D.SetDrawColor(120, 170, 255, 40);
			Graphics2D.DrawRectangle(rect);
			Graphics2D.SetDrawColor(150, 190, 255);
			Graphics2D.DrawRectangleOutline(rect);
		}

		if (dropIndex >= 0) {
			Graphics2D.SetDrawColor(FrameMarkerColor);
			Graphics2D.DrawRectangle(0, dropIndex * ROW_HEIGHT - ScrollOffset - 1, width, 2);
		}
	}

	private void PaintMark(KeyMark mark, float y, float width) {
		float x = (float)FrameToX(mark.Time * FPS);
		if (x < -KEY_WIDTH || x > width + KEY_WIDTH) return;
		var rect = RectangleF.XYWH(x - KEY_WIDTH / 2, y + 4, KEY_WIDTH, ROW_HEIGHT - 8);
		Graphics2D.SetDrawColor(mark.Selected ? mark.Color.Adjust(0, -0.3f, 0.4f) : mark.Color);
		Graphics2D.DrawRectangle(rect);
		if (mark.Selected) {
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawRectangleOutline(RectangleF.XYWH(rect.X - 1, rect.Y - 1, rect.W + 2, rect.H + 2), 2);
		}
		else {
			Graphics2D.SetDrawColor(10, 10, 10);
			Graphics2D.DrawRectangleOutline(rect);
		}
	}

	// Straight for linear, curved for bezier, dotted for stepped, nothing when the value doesn't change
	private void PaintLines(TimelineRow row, float y, float width) {
		var curve = row.Curves[0];
		var color = row.Timeline!.Color;
		Graphics2D.SetDrawColor(color.R, color.G, color.B, 170);
		float mid = y + ROW_HEIGHT / 2;
		for (int i = 0; i + 1 < curve.Count; i++) {
			if (curve.KeyAt(i) is not Keyframe<float> key) return;
			float x0 = (float)FrameToX(key.Time * FPS) + KEY_WIDTH / 2;
			float x1 = (float)FrameToX(curve.KeyAt(i + 1).GetTime() * FPS) - KEY_WIDTH / 2;
			if (x1 < 0 || x1 <= x0) continue;
			if (x0 > width) break;
			if (SameValue(row.Curves, i)) continue;

			switch (key.Interpolation) {
				case KeyframeInterpolation.Constant:
					Graphics2D.DrawDottedLine(new(x0, mid), new(x1, mid), 3);
					break;
				case KeyframeInterpolation.Linear:
					Graphics2D.DrawLine(x0, mid, x1, mid);
					break;
				default:
					float w = x1 - x0;
					Graphics2D.DrawCubicBezier(new(x0, mid + 4), new(x0 + w * 0.4f, mid + 4), new(x0 + w * 0.6f, mid - 4), new(x1, mid - 4));
					break;
			}
		}
	}

	private static bool SameValue(IFCurve[] curves, int i) {
		foreach (var curve in curves) {
			if (i + 1 >= curve.Count || curve.KeyAt(i) is not Keyframe<float> key || curve.KeyAt(i + 1) is not Keyframe<float> next || key.Value != next.Value)
				return false;
		}
		return true;
	}

	// Orange diamonds on keyed frames, orange line on the last one
	protected override void PaintTimeOverlay(float width, float height) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null || Rows.Shown.Count == 0) return;
		PaintRulerKeys(this, Rows.Shown[0].Marks, animation, width, height);
		PaintLoop(height);
	}

	internal static void PaintRulerKeys(BaseTimelineView view, IEnumerable<KeyMark> marks, EditorAnimation animation, float width, float height) {
		Graphics2D.SetDrawColor(END_COLOR);
		foreach (var mark in marks) {
			float x = (float)view.FrameToX(mark.Time * FPS);
			if (x < 0 || x > width) continue;
			float centerY = height - 7;
			float radius = 4;
			Graphics2D.DrawTriangle(new(x, centerY - radius), new(x - radius, centerY), new(x + radius, centerY));
			Graphics2D.DrawTriangle(new(x - radius, centerY), new(x, centerY + radius), new(x + radius, centerY));
		}
		float endX = (float)view.FrameToX(animation.CalculateMaxTime() * FPS);
		if (endX >= 0 && endX <= width)
			Graphics2D.DrawLine(endX, height / 2, endX, height);
	}

	protected override void PaintPanelOverlay(float width, float height) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null) return;
		float endX = (float)FrameToX(animation.CalculateMaxTime() * FPS);
		Graphics2D.SetDrawColor(END_COLOR);
		Graphics2D.DrawLine(endX, 0, endX, height);
		PaintLoop(height);
	}

	private void PaintLoop(float height) {
		Graphics2D.SetDrawColor(80, 220, 80);
		foreach (var frame in new[] { Timeline.LoopStart, Timeline.LoopEnd }) {
			if (frame is double loopFrame) {
				float x = (float)FrameToX(loopFrame);
				Graphics2D.DrawLine(x, 0, x, height);
			}
		}
	}

	protected override void OnThink() {
		base.OnThink();
		Rows.Refresh();
		if (FollowPlayhead && Timeline.PlayDirection != 0 && keysPanel != null) {
			float x = (float)FrameToX(Timeline.Frame * FPS);
			float w = keysPanel.GetRenderBounds().W;
			if (x < 0 || x > w)
				FrameOffset += x - w * 0.1f;
		}
	}

	// Keys area input

	enum DragMode { None, Pending, Keys, Box, Scale, Pan }

	DragMode mode;
	Vector2F pressPos;
	bool pressCtrl;
	bool pressShift;
	bool pressAlt;
	bool pressOnKey;
	bool dragged;
	TimelineRow? pressRow;
	double pressTime;
	double lastMoveTime;
	Vector2F lastMouse;

	/// <summary>
	/// The box selection, X/W in frames and Y/H in content pixels
	/// </summary>
	RectangleF? box;
	double boxStartFrame;
	float boxStartY;
	bool scaleRight;
	double scaleFixed;
	double scaleEdge;

	KeyEdit? edit;
	List<KeyEdit.Key> moving = [];

	double lastClickTime = -1;
	(TimelineRow Row, double Time)? lastClick;

	private TimelineRow? RowAt(float y) {
		var shown = Rows.Shown;
		int index = (int)Math.Floor((y + ScrollOffset) / ROW_HEIGHT);
		return index >= 0 && index < shown.Count ? shown[index] : null;
	}

	private KeyMark? MarkAt(TimelineRow row, float x) {
		KeyMark? best = null;
		float bestDist = KEY_HIT;
		foreach (var mark in row.Marks) {
			float distance = Math.Abs((float)FrameToX(mark.Time * FPS) - x);
			if (distance <= bestDist) {
				bestDist = distance;
				best = mark;
			}
		}
		return best;
	}

	private bool OnBoxEdge(Vector2F pos, out bool right) {
		right = false;
		if (box is not RectangleF area) return false;
		float y = pos.Y + ScrollOffset;
		if (y < area.Y || y > area.Y + area.H) return false;
		float x0 = (float)FrameToX(area.X), x1 = (float)FrameToX(area.X + area.W);
		if (Math.Abs(pos.X - x1) <= KEY_HIT) {
			right = true;
			return true;
		}
		return Math.Abs(pos.X - x0) <= KEY_HIT;
	}

	private double FrameAt(float x, bool snap) => snap ? XToFrame(x) : XToFrameExact(x);

	private void Focus() {
		Focused = true;
		ModelEditor.Active.Graph.Focused = false;
	}

	private bool KeysPress(ButtonCode button, Vector2F pos, FrameState state) {
		Focus();
		Rows.ComputeMarks();
		pressPos = lastMouse = pos;
		dragged = false;
		lastMoveTime = EngineCore.Level.Realtime;

		if (button == ButtonCode.Mouse2) {
			mode = DragMode.Pan;
			return true;
		}
		if (button != ButtonCode.Mouse1) return true;

		pressCtrl = state.Keyboard.ControlDown;
		pressShift = state.Keyboard.ShiftDown;
		pressAlt = state.Keyboard.AltDown;
		pressRow = RowAt(pos.Y);
		var mark = pressRow == null ? null : MarkAt(pressRow, pos.X);
		pressOnKey = mark != null;
		pressTime = mark?.Time ?? 0;
		mode = DragMode.Pending;

		// box edge takes priority over a key sitting on it
		if (OnBoxEdge(pos, out scaleRight) && ModelEditor.Active.KeyframesSelected) {
			var area = box!.Value;
			scaleFixed = scaleRight ? area.X : area.X + area.W;
			scaleEdge = scaleRight ? area.X + area.W : area.X;
			mode = DragMode.Scale;
			return true;
		}

		// Pressing an unselected key selects it right away, for dragging.
		if (pressOnKey && (!pressCtrl || pressShift) && !mark!.Value.Selected)
			ModelEditor.Active.SelectKeyframes(Rows.KeysAt(pressRow!, pressTime).ToList());
		return true;
	}

	private bool KeysDrag(Vector2F pos, FrameState state) {
		if (pos != lastMouse) {
			lastMouse = pos;
			lastMoveTime = EngineCore.Level.Realtime;
		}
		bool snap = !state.Keyboard.ShiftDown;

		switch (mode) {
			case DragMode.Pan:
				var delta = state.Mouse.MouseDelta;
				FrameOffset -= delta.X;
				ScrollOffset -= delta.Y;
				return true;
			case DragMode.Pending:
				if ((pos - pressPos).Length < DRAG_START) return true;
				dragged = true;
				// ctrl alone boxes from on top of a key, ctrl+shift duplicates
				if (pressOnKey && (!pressCtrl || pressShift))
					BeginKeyDrag(pressCtrl && pressShift);
				else {
					mode = DragMode.Box;
					boxStartFrame = FrameAt(pressPos.X, snap);
					boxStartY = pressPos.Y + ScrollOffset;
				}
				return KeysDrag(pos, state);
			case DragMode.Keys:
				UpdateKeyDrag(pos.X, snap);
				return true;
			case DragMode.Box:
				double frame = FrameAt(pos.X, snap);
				float y = Math.Clamp(pos.Y + ScrollOffset, 0, Rows.Shown.Count * ROW_HEIGHT);
				box = RectangleF.XYWH((float)Math.Min(boxStartFrame, frame), Math.Min(boxStartY, y), (float)Math.Abs(frame - boxStartFrame), Math.Abs(y - boxStartY));
				return true;
			case DragMode.Scale:
				if (!dragged) {
					if ((pos - pressPos).Length < 1) return true;
					dragged = true;
					BeginScale();
				}
				UpdateScale(FrameAt(pos.X, snap), snap);
				return true;
		}
		return true;
	}

	private bool KeysRelease(ButtonCode button, Vector2F pos, FrameState state) {
		var was = mode;
		mode = DragMode.None;
		switch (was) {
			case DragMode.Keys:
			case DragMode.Scale:
				if (edit != null) {
					edit.ResolveOverlaps(moving);
					AnimationRows.Select(moving.Where(k => edit.Find(k.Primary) != null));
				}
				edit = null;
				moving = [];
				Rows.MarkDirty();
				break;
			case DragMode.Box:
				SelectInBox(pressCtrl);
				// a quick flick leaves no box behind
				if (EngineCore.Level.Realtime - lastMoveTime < BOX_KEEP_PAUSE)
					box = null;
				break;
			case DragMode.Pending:
				Click(pos, state);
				break;
		}
		return true;
	}

	private static bool SameRow(TimelineRow first, TimelineRow second)
		=> first.Kind == second.Kind && first.Owner == second.Owner && first.Timeline == second.Timeline;

	private void Click(Vector2F pos, FrameState state) {
		var now = EngineCore.Level.Realtime;
		if (pressOnKey && pressRow != null) {
			var keys = Rows.KeysAt(pressRow, pressTime).ToList();
			bool doubleClick = lastClick != null && SameRow(lastClick.Value.Row, pressRow) && Math.Abs(lastClick.Value.Time - pressTime) <= KeyEdit.TIME_EPSILON
				&& now - lastClickTime < DOUBLE_CLICK;
			lastClick = doubleClick ? null : (pressRow, pressTime);
			lastClickTime = now;

			if (doubleClick) {
				ModelEditor.Active.SelectKeyframes(keys);
				Rows.DeleteSelected();
				return;
			}
			if (pressCtrl) {
				if (keys.All(ModelEditor.Active.IsKeyframeSelected))
					ModelEditor.Active.UnselectKeyframes(keys);
				else
					ModelEditor.Active.SelectKeyframes(keys, true);
			}
			else {
				ModelEditor.Active.SelectKeyframes(keys);
				Timeline.SetFrame(pressTime * FPS);
			}
			// With the dopesheet locked, clicking a key selects its item.
			if (Rows.Locked && pressRow.Kind == RowKind.Property && pressRow.Owner != null)
				ModelEditor.Active.SelectObject(pressRow.Owner);
			return;
		}

		lastClick = null;
		box = null;
		if (!pressCtrl)
			ModelEditor.Active.UnselectAllKeyframes();
		Timeline.SetFrame(FrameAt(pos.X, !state.Keyboard.ShiftDown));
	}

	private void SelectInBox(bool additive) {
		if (box is not RectangleF area) return;
		var shown = Rows.Shown;
		List<IKeyframe> keys = [];
		for (int i = 0; i < shown.Count; i++) {
			float top = i * ROW_HEIGHT;
			if (top + ROW_HEIGHT < area.Y || top > area.Y + area.H) continue;
			foreach (var mark in shown[i].Marks) {
				double frame = mark.Time * FPS;
				if (frame >= area.X - KeyEdit.TIME_EPSILON && frame <= area.X + area.W + KeyEdit.TIME_EPSILON)
					keys.AddRange(Rows.KeysAt(shown[i], mark.Time));
			}
		}
		ModelEditor.Active.SelectKeyframes(keys, additive);
	}

	private void BeginKeyDrag(bool duplicate) {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null) {
			mode = DragMode.None;
			return;
		}
		mode = DragMode.Keys;
		bool shiftMode = ShiftKeys || pressAlt;

		List<EditorTimeline> timelines;
		if (shiftMode) {
			timelines = [];
			foreach (var row in Rows.All) {
				if (row.Kind == RowKind.Property)
					timelines.Add(row.Timeline!);
			}
		}
		else
			timelines = Rows.SelectedTimelines();
		edit = new KeyEdit(animation, timelines);
		moving = [];
		foreach (var key in edit.Keys) {
			if (AnimationRows.IsSelected(key))
				moving.Add(key);
		}
		if (shiftMode)
			moving.AddRange(edit.Keys.Where(
				k => k.OriginalTime > pressTime + KeyEdit.TIME_EPSILON && !moving.Contains(k)));
		if (duplicate) {
			List<KeyEdit.Key> duplicates = [];
			foreach (var key in moving)
				duplicates.Add(edit.Duplicate(key));
			moving = duplicates;
			AnimationRows.Select(moving);
		}
	}

	private void UpdateKeyDrag(float x, bool snap) {
		if (edit == null) return;
		double start = pressTime * FPS;
		double target = start + XToFrameExact(x) - XToFrameExact(pressPos.X);
		if (snap)
			target = Math.Round(target);
		double delta = (target - start) / FPS;
		foreach (var key in moving)
			edit.SetTime(key, key.OriginalTime + delta);
		edit.Finish();
	}

	private void BeginScale() {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (animation == null) {
			mode = DragMode.None;
			return;
		}
		edit = new KeyEdit(animation, Rows.SelectedTimelines());
		moving = [];
		foreach (var key in edit.Keys) {
			if (AnimationRows.IsSelected(key))
				moving.Add(key);
		}
	}

	// Keys keep their place between the fixed edge and the dragged one
	private void UpdateScale(double edgeFrame, bool snap) {
		if (edit == null || box is not RectangleF area) return;
		double span = scaleEdge - scaleFixed;
		if (Math.Abs(span) < 1e-9) return;
		double scale = (edgeFrame - scaleFixed) / span;
		foreach (var key in moving) {
			double frame = scaleFixed + (key.OriginalTime * FPS - scaleFixed) * scale;
			edit.SetTime(key, (snap ? Math.Round(frame) : frame) / FPS);
		}
		edit.Finish();
		double low = Math.Min(scaleFixed, edgeFrame);
		double high = Math.Max(scaleFixed, edgeFrame);
		box = RectangleF.XYWH((float)low, area.Y, (float)(high - low), area.H);
	}

	class KeysPanel(DopesheetView view) : Panel(view)
	{
		public override void Paint(float width, float height) => view.PaintKeys(width, height);
		protected override bool MouseClick(FrameState state, ButtonCode button) => view.KeysPress(button, GetMousePos(), state);
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) => view.KeysDrag(GetMousePos(), state);
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) => view.KeysRelease(button, GetMousePos(), state);
		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			view.ScrollOffset -= delta.Y * ROW_HEIGHT * 2;
			return true;
		}
	}

	// Names column

	int dropIndex = -1;
	TimelineRow? namePressRow;
	Vector2F namePressPos;
	bool nameDragging;

	/// <summary>
	/// Draws the row names, <paramref name="indent"/> leaves room for the graph's dots.
	/// </summary>
	internal static void PaintRowNames(AnimationRows rows, float scroll, float width, float height, float indent) {
		var shown = rows.Shown;
		for (int i = Math.Max(0, (int)(scroll / ROW_HEIGHT)); i < shown.Count; i++) {
			float y = i * ROW_HEIGHT - scroll;
			if (y > height) break;
			var row = shown[i];

			Graphics2D.SetDrawColor(RowColor(row));
			Graphics2D.DrawRectangle(0, y, width, ROW_HEIGHT);
			Graphics2D.SetDrawColor(0, 0, 0, 120);
			Graphics2D.DrawLine(0, y + ROW_HEIGHT - 1, width, y + ROW_HEIGHT - 1);

			float mid = y + ROW_HEIGHT / 2;
			float textX = indent + 8;
			switch (row.Kind) {
				case RowKind.Group:
					Graphics2D.SetDrawColor(220, 220, 220);
					Graphics2D.DrawRectangle(indent + 5, mid - 1, 10, 2);
					if (rows.IsCollapsed(row.Owner!))
						Graphics2D.DrawRectangle(indent + 9, mid - 5, 2, 10);
					DrawIcon(row.Icon, indent + 22, mid);
					textX = indent + 44;
					break;
				case RowKind.Property:
					bool nested = row.Parent?.Kind == RowKind.Group;
					DrawIcon(row.Icon, indent + (nested ? 30 : 8), mid);
					textX = indent + (nested ? 52 : 30);
					break;
			}
			Graphics2D.SetDrawColor(235, 235, 235);
			Graphics2D.DrawText(textX, mid, row.Name, Graphics2D.UI_FONT_NAME, 18, Anchor.CenterLeft);
		}
	}

	private void PaintNames(float width, float height) {
		PaintRowNames(Rows, ScrollOffset, width, height, 0);
		if (dropIndex >= 0) {
			Graphics2D.SetDrawColor(FrameMarkerColor);
			Graphics2D.DrawRectangle(0, dropIndex * ROW_HEIGHT - ScrollOffset - 1, width, 2);
		}
	}

	private static void DrawIcon(string? icon, float x, float mid) {
		if (icon == null) return;
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.SetTexture(textures.LoadTextureFromFile(icon));
		Graphics2D.DrawTexturedRectangle(new Vector2F(x, mid - 8), new Vector2F(16));
	}

	private bool NamesPress(ButtonCode button, Vector2F pos) {
		Focus();
		namePressRow = RowAt(pos.Y);
		namePressPos = pos;
		nameDragging = false;
		if (namePressRow == null) return true;

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
		dropIndex = GroupDropIndex(Rows, pos.Y + ScrollOffset);
		return true;
	}

	/// <summary>
	/// Row index a dragged bone row would land on, only between bone rows
	/// </summary>
	internal static int GroupDropIndex(AnimationRows rows, float contentY) {
		var shown = rows.Shown;
		int index = Math.Clamp((int)Math.Round(contentY / ROW_HEIGHT), 1, shown.Count);
		while (index < shown.Count && shown[index].Kind == RowKind.Property && shown[index].Parent?.Kind == RowKind.Group)
			index++;
		return index;
	}

	internal static void DropGroup(AnimationRows rows, TimelineRow row, int dropIndex) {
		var shown = rows.Shown;
		int groupsBefore = 0;
		for (int i = 0; i < Math.Min(dropIndex, shown.Count); i++) {
			if (shown[i].Kind == RowKind.Group && shown[i] != row)
				groupsBefore++;
		}
		rows.MoveGroup(row.Owner!, groupsBefore);
	}

	/// <summary>
	/// Selects the row's item, and the tool for transform properties.
	/// </summary>
	internal static void ClickRowName(TimelineRow row, bool ctrl) {
		switch (row.Kind) {
			case RowKind.Overview:
				ModelEditor.Active.SelectObject(row.Owner!);
				break;
			case RowKind.Group:
			case RowKind.Property:
				if (row.Owner == null) break;
				if (ctrl && ModelEditor.Active.IsObjectSelected(row.Owner))
					ModelEditor.Active.UnselectObject(row.Owner);
				else
					ModelEditor.Active.SelectObject(row.Owner, ctrl);
				var tool = row.Property switch {
					KeyframeProperty.Bone_Rotation => EditorDefaultOperator.RotateSelection,
					KeyframeProperty.Bone_Translation => EditorDefaultOperator.TranslateSelection,
					KeyframeProperty.Bone_Scale => EditorDefaultOperator.ScaleSelection,
					KeyframeProperty.Bone_Shear => EditorDefaultOperator.ShearSelection,
					_ => (EditorDefaultOperator?)null
				};
				if (tool is EditorDefaultOperator toolOperator)
					ModelEditor.Active.Editor.SetEditorOperator(toolOperator);
				break;
		}
	}

	private bool NamesRelease(ButtonCode button, Vector2F pos, FrameState state) {
		var row = namePressRow;
		namePressRow = null;
		if (nameDragging && row != null) {
			DropGroup(Rows, row, dropIndex);
			dropIndex = -1;
			nameDragging = false;
			return true;
		}
		if (row != null && button == ButtonCode.Mouse1)
			ClickRowName(row, state.Keyboard.ControlDown);
		return true;
	}

	class NamesPanel(DopesheetView view, Element parent) : Panel(parent)
	{
		public override void Paint(float width, float height) => view.PaintNames(width, height);
		protected override bool MouseClick(FrameState state, ButtonCode button) => view.NamesPress(button, GetMousePos());
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) => view.NamesDrag(GetMousePos());
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) => view.NamesRelease(button, GetMousePos(), state);

		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			view.ScrollOffset -= delta.Y * ROW_HEIGHT * 2;
			return true;
		}
	}
}
