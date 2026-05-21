using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu;

public class MainMenuButton : Button
{
	Image image;
	public MainMenuButton(Element? parent, string icon) : base(parent) {
		SetTextAlignment(Anchor.CenterRight);
		Clipping = false;
		Roundness = 4;
		image = new(this);
		image.Anchor = Anchor.CenterLeft;
		image.Origin = Anchor.CenterLeft;
		image.SetTexture(Level.Textures.LoadTextureFromFile(icon));
	}

	public string SubText;
	SecondOrderSystem sos = new SecondOrderSystem(1, 1, 1, 100);

	public void SetStart(float x) => sos.ResetTo(x);

	public float Offscreen { get; set; }

	protected override void OnThink() {
		base.OnThink();
		ChildRenderOffset = new(sos.Update(Offscreen != 0 ? EngineCore.GetWindowWidth() / 2 * Offscreen : IsHovered() ? -50 : 0), 0);
	}


	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		image.SetSize(new(height, height));
	}


	public override void Paint(float width, float height) {
		ColorStateSetup(out Color back, out Color fore);

		var decomposed = fore.Adjust(0, 0, 2555, false);

		Graphics2D.SetDrawColor(decomposed);
		var p = 2;

		// ImageOrientation = ImageOrientation.None;
		// ImageColor = GetFgColor().Adjust(0, -0.2, 2, false);
		// ImageDrawing(new(p / 2, p / 2), new(height - p * 2, height - p * 2));

		Graphics2D.DrawText(new(width - 8, 8), GetText(), GetFont(), GetTextSize() * 0.85f, Anchor.TopRight);
		if (SubText != null)
			Graphics2D.DrawText(new(width - 4, height - 8), SubText, GetFont(), GetTextSize() * 0.45f, Anchor.BottomRight);
	}
}
