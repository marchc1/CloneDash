using CloneDash.Common.UI;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace CloneDash.Menu
{
	public class FooterButton : Button
	{
		public Action? Action { get; set; }

		private readonly SecondOrderSystem _sos = new(3, 1, 1, 60);

		protected override void Initialize() {
			base.Initialize();
			ShouldDrawImage = false;
			Clipping = false;
			BorderSize = 3;
			Size = new Vector2F(200, 60);
			MouseReleaseEvent += (_, _, _) => Action?.Invoke();
		}

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

			float y = _sos.Update(Action != null ? -12 : 60);
			Position = new Vector2F(Position.X, y);
		}

		public override void Paint(float width, float height) {
			PaintBackground(this, width, height, BackgroundColor, ForegroundColor, BorderSize, 8);

			Graphics2D.SetDrawColor(ForegroundColor);

			bool right = Anchor == Anchor.BottomRight;

			const float iconSize = 24;

			float textHeight = CloneDashUI.GetFontSize(20);
			Vector2F size = Graphics2D.DrawText(new Vector2F(32 + (right ? 0 : iconSize + 20), height / 2f), Text, CloneDashUI.FontBold, textHeight, Anchor.CenterLeft);
			Size = new Vector2F(size.X + 64 + 20 + iconSize, Size.Y);
			
			Graphics2D.SetTexture(Image);
			Graphics2D.DrawImage(new Vector2F(32 + (right ? 20 + size.X : 0), (height - iconSize) / 2f), new Vector2F(iconSize));
		}
	}
}