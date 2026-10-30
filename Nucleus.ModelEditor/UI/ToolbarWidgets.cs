using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace Nucleus.ModelEditor.UI
{
	public static class ToolbarButtons
	{
		/// <summary>
		/// Puts an icon on the left side of a button and left-aligns its text.
		/// </summary>
		public static Button WithIcon(this Button button, string icon, bool dim = false) {
			var iconImage = new ToolIcon(button, icon, dim);
			iconImage.Dock = Dock.Left;
			iconImage.Size = new(18);
			iconImage.DockMargin = RectangleF.TLRB(4, 6, 0, 4);

			button.SetTextAlignment(Anchor.CenterLeft);
			button.SetTextPadding(new(26, 0));
			return button;
		}

		public static Button New(Element parent, string text, string icon) {
			var button = new Button(parent);
			button.Text = text;
			return button.WithIcon(icon);
		}
	}

	public class ToolIcon : Panel
	{
		readonly string icon;
		readonly bool dim;

		public ToolIcon(Element parent, string icon, bool dim) : base(parent) {
			this.icon = icon;
			this.dim = dim;
			SetPassthru(true);
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
		}

		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(255, 255, 255, dim ? 90 : 255);
			var texture = textures.LoadTextureFromFile(icon);
			Graphics2D.SetTexture(texture);

			// fit inside the box without stretching
			float aspect = texture.GetWidth() / (float)Math.Max(texture.GetHeight(), 1);
			float w = Math.Min(width, height * aspect), h = w / aspect;
			Graphics2D.DrawTexturedRectangle(new Vector2F((width - w) / 2, (height - h) / 2), new(w, h));
		}
	}

	/// <summary>
	/// The big SETUP / ANIMATE label in the top left of the viewport, click to switch modes
	/// </summary>
	public class ModeLabel : Button
	{
		public ModeLabel(Element parent) : base(parent) {
			TextSize = 30;
			BorderSize = 0;
		}

		public override void PaintBackground(float width, float height) {
			if (!IsHovered()) return;
			Graphics2D.SetDrawColor(255, 255, 255, IsDepressed() ? 30 : 15);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}

		public override void PaintBorder(float width, float height) { }

		public override void Paint(float width, float height) {
			bool animate = ModelEditor.Active.AnimationMode;
			var color = animate ? new Color(255, 170, 70) : new Color(80, 215, 235);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(textures.LoadTextureFromFile(
				animate ? "models/mode_animate.png" : "models/mode_setup.png"));
			Graphics2D.DrawTexturedRectangle(new Vector2F(4, (height - 36) / 2), new(36));
			Graphics2D.SetDrawColor(color);
			Graphics2D.DrawText(new(48, height / 2), Text, Graphics2D.UI_FONT_NAME, TextSize, Anchor.CenterLeft);
		}
	}

	public enum ViewportItemKind
	{
		Bones,
		Images,
		Others
	}

	/// <summary>
	/// Per-kind select/show/names toggles for the viewport
	/// </summary>
	public class ViewportOptionsGrid : Panel
	{
		public readonly bool[] Selectable = [true, true, true];
		public readonly bool[] Visible = [true, true, true];
		public readonly bool[] ShowNames = [false, false, false];

		public event Action? Changed;

		private const float LABEL_WIDTH = 64;
		private const float HEADER_HEIGHT = 20;
		private static readonly string[] rowNames = ["Bones", "Images", "Others"];
		private static readonly string[] columnIcons = ["models/select.png", "models/viseye.png", "models/nametag.png"];

		public ViewportOptionsGrid(Element parent) : base(parent) {
			DockPadding = RectangleF.TLRB(4);
		}

		private bool[] Column(int i) => i switch { 0 => Selectable, 1 => Visible, _ => ShowNames };

		private bool CellAt(Vector2F pos, out int row, out int col) {
			var content = GetRenderBounds();
			float cellW = (content.W - LABEL_WIDTH - 8) / 3, cellH = (content.H - HEADER_HEIGHT - 8) / 3;
			col = (int)MathF.Floor((pos.X - 4 - LABEL_WIDTH) / cellW);
			row = (int)MathF.Floor((pos.Y - 4 - HEADER_HEIGHT) / cellH);
			return col >= 0 && col < 3 && row >= 0 && row < 3;
		}

		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			if (button != ButtonCode.Mouse1 || !CellAt(GetMousePos(), out int row, out int col)) return true;
			Column(col)[row] = !Column(col)[row];
			Changed?.Invoke();
			return true;
		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
			float cellW = (width - LABEL_WIDTH - 8) / 3, cellH = (height - HEADER_HEIGHT - 8) / 3;
			int hoveredRow = -1, hoveredColumn = -1;
			if (!IsHovered() || !CellAt(GetMousePos(), out hoveredRow, out hoveredColumn)) {
				hoveredRow = -1;
				hoveredColumn = -1;
			}

			for (int column = 0; column < 3; column++) {
				float centerX = 4 + LABEL_WIDTH + cellW * column + cellW / 2;
				Graphics2D.SetDrawColor(hoveredColumn == column ? 255 : 190, hoveredColumn == column ? 255 : 190, hoveredColumn == column ? 255 : 200);
				Graphics2D.SetTexture(textures.LoadTextureFromFile(columnIcons[column]));
				Graphics2D.DrawTexturedRectangle(new Vector2F(centerX - 8, 4 + (HEADER_HEIGHT - 16) / 2), new(16));
			}

			for (int row = 0; row < 3; row++) {
				float centerY = 4 + HEADER_HEIGHT + cellH * row + cellH / 2;
				Graphics2D.SetDrawColor(hoveredRow == row ? 235 : 165, hoveredRow == row ? 235 : 170, hoveredRow == row ? 240 : 180);
				Graphics2D.DrawText(new(8, centerY), rowNames[row], Graphics2D.UI_FONT_NAME, 17, Anchor.CenterLeft);

				for (int column = 0; column < 3; column++) {
					float centerX = 4 + LABEL_WIDTH + cellW * column + cellW / 2;
					bool on = Column(column)[row];
					if (hoveredRow == row && hoveredColumn == column) {
						Graphics2D.SetDrawColor(255, 255, 255, 20);
						Graphics2D.DrawRectangle(centerX - cellW / 2 + 1, centerY - cellH / 2 + 1, cellW - 2, cellH - 2);
					}
					if (on) {
						Graphics2D.SetDrawColor(220, 225, 235);
						Graphics2D.DrawCircle(new(centerX, centerY), 3.5f);
					}
					else {
						Graphics2D.SetDrawColor(110, 115, 125);
						Graphics2D.DrawCircle(new(centerX, centerY), 1.5f);
					}
				}
			}
		}

	}

	public class ZoomStrip : Panel
	{
		readonly EditorPanel editor;

		public ZoomStrip(EditorPanel editor) : base(editor) {
			this.editor = editor;
			DockPadding = RectangleF.TLRB(2);

			Button("1:1", Dock.Top, "Actual size", () => editor.ZoomAt(1));
			Button("+", Dock.Top, "Zoom in", () => editor.ZoomAt(editor.CameraZoom * 1.25f));
			var fit = new FitButton(this);
			fit.Dock = Dock.Bottom;
			fit.Size = new(26);
			fit.BorderSize = 0;
			fit.TooltipText = "Fit to view (Home)";
			fit.OnButtonClick += (_, _) => editor.FitToView();
			Button("−", Dock.Bottom, "Zoom out", () => editor.ZoomAt(editor.CameraZoom / 1.25f));

			var slider = new ZoomSlider(this, editor);
			slider.Dock = Dock.Fill;
		}

		private Button Button(string text, Dock dock, string tip, Action click) {
			var button = new Button(this, text);
			button.Dock = dock;
			button.Size = new(26);
			button.TextSize = text.Length == 1 ? 24 : 16;
			button.BorderSize = 0;
			button.TooltipText = tip;
			button.OnButtonClick += (_, _) => click();
			return button;
		}
	}

	public class FitButton(Element parent) : Button(parent, "")
	{
		public override void Paint(float w, float h) {
			Graphics2D.SetDrawColor(IsHovered() ? new Color(255, 255, 255) : new Color(190, 195, 205));
			float margin = 7;
			float length = 5;
			var corners = new[] {
				(X: margin, Y: margin, DirX: 1, DirY: 1),
				(X: w - margin, Y: margin, DirX: -1, DirY: 1),
				(X: margin, Y: h - margin, DirX: 1, DirY: -1),
				(X: w - margin, Y: h - margin, DirX: -1, DirY: -1)
			};
			foreach (var corner in corners) {
				Graphics2D.DrawLine(corner.X, corner.Y, corner.X + corner.DirX * length, corner.Y, 2);
				Graphics2D.DrawLine(corner.X, corner.Y, corner.X, corner.Y + corner.DirY * length, 2);
			}
		}
	}

	public class ZoomSlider : Panel
	{
		readonly EditorPanel editor;

		public ZoomSlider(Element parent, EditorPanel editor) : base(parent) {
			this.editor = editor;
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
			TooltipText = "Zoom";
		}

		private const float PAD = 10;

		private static float ToT(float zoom) => (MathF.Log(zoom) - MathF.Log(EditorPanel.MIN_ZOOM))
				/ (MathF.Log(EditorPanel.MAX_ZOOM) - MathF.Log(EditorPanel.MIN_ZOOM));
		private static float FromT(float t)
			=> MathF.Exp(MathF.Log(EditorPanel.MIN_ZOOM) + Math.Clamp(t, 0, 1) * (MathF.Log(EditorPanel.MAX_ZOOM) - MathF.Log(EditorPanel.MIN_ZOOM)));

		private void SetFromMouse() {
			float height = GetRenderBounds().H;
			float t = 1 - (GetMousePos().Y - PAD) / MathF.Max(height - PAD * 2, 1);
			editor.ZoomAt(FromT(t));
		}

		protected override bool MouseClick(FrameState state, ButtonCode button) {
			if (button == ButtonCode.Mouse1)
				SetFromMouse();
			return true;
		}

		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			SetFromMouse();
			return true;
		}

		public override void Paint(float width, float height) {
			float x = width / 2;
			float top = PAD, bottom = height - PAD;
			Graphics2D.SetDrawColor(110, 115, 125);
			Graphics2D.DrawLine(x, top, x, bottom, 2);

			// 1:1 tick
			float one = bottom - ToT(1) * (bottom - top);
			Graphics2D.DrawLine(x - 5, one, x + 5, one, 1);

			float y = bottom - ToT(editor.CameraZoom) * (bottom - top);
			Graphics2D.SetDrawColor(IsHovered() || IsDepressed() ? new Color(255, 255, 255) : new Color(200, 205, 215));
			Graphics2D.DrawCircle(new(x, y), 5);
		}
	}
}
