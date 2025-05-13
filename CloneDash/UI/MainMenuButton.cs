using CloneDash.Animation;

using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;

using Raylib_cs;

namespace CloneDash.UI;

public class MainMenuButton : Button
{
	protected override void Initialize() {
		base.Initialize();
		TextAlignment = Anchor.CenterRight;
		ShouldDrawImage = false;
		Clipping = false;
	}

	public string SubText;
	SecondOrderSystem sos = new SecondOrderSystem(1, 1, 1, 100);

	public void SetStart(float x) => sos.ResetTo(x);

	public float Offscreen { get; set; }

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		ChildRenderOffset = new(sos.Update(Offscreen != 0 ? frameState.WindowWidth / 2 * Offscreen : Hovered ? -50 : 0), 0);
	}

	public override void Paint(float width, float height) {
		ColorStateSetup(this, out Color back, out Color fore);
		PaintBackground(this, width, height, back, fore, BorderSize);

		var decomposed = fore.Adjust(0, 0, 2555, false);

		Graphics2D.SetDrawColor(decomposed);
		var p = 2;

		ImageOrientation = ImageOrientation.None;
		ImageColor = ForegroundColor.Adjust(0, -0.2, 2, false);
		ImageDrawing(new(p / 2, p / 2), new(height - p * 2, height - p * 2));

		Graphics2D.DrawText(new(width - 8, 8), Text, Font, TextSize * 0.85f, Anchor.TopRight);
		if (SubText != null)
			Graphics2D.DrawText(new(width - 4, height - 8), SubText, Font, TextSize * 0.45f, Anchor.BottomRight);
	}
}
