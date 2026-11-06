using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Common.Game;

public class PauseMenuButton : Button
{
	Image? iconImage;
	public PauseMenuButton(Element parent, string image) : base(parent) {
		if (image != null) {
			iconImage = new Image(this);
			iconImage.Texture = textures.LoadTextureFromFile(image);
			iconImage.ImageOrientation = ImageOrientation.Zoom;
			iconImage.ImagePadding = new(4);
			iconImage.Dock = Dock.Left;
		}
	}

	public override void Paint(float width, float height) {
		var backpre = GetBgColor();

		var back = Element.MixColorBasedOnMouseState(this, backpre, new(0, 0.8f, 2.4f, 1f), new(0, 1.2f, 0.6f, 1f));
		var fore = Element.MixColorBasedOnMouseState(this, GetFgColor(), new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

		Graphics2D.SetDrawColor(back);
		Graphics2D.DrawRectangle(0, 0, width, height);
		var text = Text;
		var tSize = Graphics2D.GetTextSize(text, Font, TextSize);
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawText(new((width / 2) + (height / 4), height / 2), text, Font, TextSize, Anchor.Center);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		if (iconImage != null) {
			iconImage.Size = new(height, height);
		}
	}
}
