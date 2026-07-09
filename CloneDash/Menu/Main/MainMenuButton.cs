using CloneDash.Common.UI;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu.Main;

public class MainMenuButton : Button
{
	public const float Height = 64;
	public const float Spacing = 16;

	public float Offscreen { get; set; }

	private readonly string _subText;
	private Image image;
	private SecondOrderSystem sos = new(1, 1, 1, 100);

	public float StartOffset { set => sos.ResetTo(value); }

	public MainMenuButton(Element? parent, string text, string subtext, string icon) : base(parent) {
		Text = text;
		_subText = subtext;

		SetTextAlignment(Anchor.CenterRight);
		Clipping = false;
		Roundness = 32;
		BorderSize = 3;
		Size = new Vector2F(600, Height);

		image = new Image(this);
		image.Clipping = false;
		image.SetAnchor(Anchor.CenterLeft);
		image.SetOrigin(Anchor.CenterLeft);
		image.Texture = Level.Textures.LoadTextureFromFile(icon);
	}

	protected override void OnThink() {
		base.OnThink();

		ChildRenderOffset = new Vector2F(
			sos.Update(Offscreen != 0 ? EngineCore.GetWindowWidth() / 2 * Offscreen : IsHovered() ? -50 : 0), 0
		);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		image.Size = new Vector2F(44);
		image.Position = new Vector2F(32, 0);
	}

	public override void Paint(float width, float height) {
		ColorStateSetup(out Color _, out Color fore);
		Graphics2D.SetDrawColor(fore);
		image.ImageColor = fore;

		float topHeight = CloneDashUI.GetFontSize(20);
		float bottomHeight = CloneDashUI.GetFontSize(14);
		float totalHeight = topHeight + bottomHeight;
		float start = height / 2f - totalHeight / 2f;

		Graphics2D.DrawText(new Vector2F(width - 32, start), Text, CloneDashUI.FontBold, topHeight,
			Anchor.TopRight);
		Graphics2D.DrawText(new Vector2F(width - 32, start + topHeight), _subText, CloneDashUI.FontNormal, bottomHeight,
			Anchor.TopRight);
	}
}