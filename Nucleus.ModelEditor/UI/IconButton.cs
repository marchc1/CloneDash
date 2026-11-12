using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class IconButton : Button
	{
		private const float ICON_SIZE = 24;
		private const float INSET = 4;
		private const float GAP = 6;
		private const float END_PAD = 10;

		readonly Label label;
		public Nucleus.UI.Elements.Image? Icon { get; }

		public IconButton(Element parent, string text, string? icon = null) : base(parent, text) {
			if (icon != null) {
				Icon = new Nucleus.UI.Elements.Image(this);
				Icon.SetPassthru(true);
				Icon.Size = new(INSET + ICON_SIZE);
				Icon.Dock = Dock.Left;
				Icon.DockMargin = RectangleF.TLRB(INSET, INSET, 0, INSET);
				Icon.ImageOrientation = ImageOrientation.Zoom;
				Icon.Texture = textures.LoadTextureFromFile(icon);
			}

			label = new Label(this, text);
			label.SetPassthru(true);
			label.Dock = Dock.Fill;
			label.DockMargin = RectangleF.TLRB(0, icon == null ? END_PAD : GAP, END_PAD, 0);
			label.SetTextAlignment(icon == null ? Anchor.Center : Anchor.CenterLeft);
		}

		protected override void TextChanged(ReadOnlySpan<char> text) {
			base.TextChanged(text);
			// null during the base constructor
			if (label != null)
				label.Text = text;
		}

		/// <summary>
		/// How wide the button wants to be to fit its icon and label
		/// </summary>
		public float GetNaturalWidth() {
			float text = Graphics2D.GetTextSize(localize.Find(Text), label.Font, label.GetRenderTextSize()).W;
			// +2 so pixel snapping doesn't truncate it
			return MathF.Ceiling(text) + 2 + END_PAD + (Icon == null ? END_PAD : INSET + ICON_SIZE + GAP);
		}

		// the label child draws the text
		public override void Paint(float width, float height) { }
	}
}
