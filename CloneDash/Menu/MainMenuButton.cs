using CloneDash.Common.UI;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Menu;

public class MainMenuButton : Button
{
	public string SubText = string.Empty;
	public float Offscreen { get; set; }

	private readonly SecondOrderSystem _sos = new(1, 1, 1, 100);

	protected override void Initialize() {
		base.Initialize();
		TextAlignment = Anchor.CenterRight;
		ShouldDrawImage = false;
		Clipping = false;
		BorderSize = 3;
		Size = new Vector2F(600, 64);
	}

	public void SetStart(float x) => _sos.ResetTo(x);


	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		// ChildRenderOffset = new(sos.Update(Offscreen != 0 ? frameState.WindowWidth / 2 * Offscreen : Hovered ? -50 : 0), 0);
	}

	public override void Paint(float width, float height) {
		ColorStateSetup(this, out Color back, out Color fore);
		PaintBackground(this, width, height, back, fore, BorderSize, 32);

		Graphics2D.SetDrawColor(fore);

		float iconHeight = height * 0.8f;

		ImageOrientation = ImageOrientation.None;
		ImageColor = fore;
		ImageDrawing(new Vector2F(16, (height - iconHeight) / 2f - 3), new Vector2F(iconHeight));

		float topHeight = CloneDashUI.GetFontSize(20);
		float bottomHeight = CloneDashUI.GetFontSize(14);
		float totalHeight = topHeight + bottomHeight;
		float start = height / 2f - totalHeight / 2f;

		Graphics2D.DrawText(new Vector2F(width - 32, start), Text, CloneDashUI.FontBold, topHeight, Anchor.TopRight);
		Graphics2D.DrawText(new Vector2F(width - 32, start + topHeight), SubText, CloneDashUI.FontNormal, bottomHeight,
			Anchor.TopRight);
	}
}