using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Entities;
using Nucleus.Types;

using Raylib_cs;

using static Nucleus.NMath;

namespace CloneDash.Game
{
	public enum TextEffectTransitionOut
	{
		SlideUp,
		SlideUpThenToLeft
	}

	public class TextEffect : Entity
	{
		public TextEffect(string text, Vector2F position, TextEffectTransitionOut transitionOut, Color? c = null) {
			Text = text;
			Position = position;
			TransitionOut = transitionOut;
			if (c.HasValue)
				Color = c.Value;
		}

		public bool SuppressAutoDeath { get; set; } = false;

		public string Text { get; set; } = "Not Set???";
		public TextEffectTransitionOut TransitionOut { get; set; }
		public Color Color { get; set; } = new(255, 255, 255, 255);

		public override void PostRender(FrameState frameState) {
			float ageToDie = 0.6f;
			double lifetime;

			if (SuppressAutoDeath)
				lifetime = 0;
			else {
				lifetime = this.Lifetime;
				if (lifetime > ageToDie) {
					this.Remove();
					return;
				}
			}

			var finalPos = Position / DashGameLevel.GlobalScale;
			var pos0to1 = Ease.OutExpo(Raymath.Remap((float)lifetime, 0, ageToDie, 0, 1));
			var pos0to1_two = TransitionOut == TextEffectTransitionOut.SlideUpThenToLeft ? Ease.InExpo(Raymath.Remap(Math.Clamp((float)lifetime, ageToDie / 2, ageToDie), ageToDie / 2, ageToDie, 0, 1)) : 0;

			var pos = pos0to1 * frameState.WindowHeight * 0.2f;
			var size = 1f - (float)Ease.InExpo(Remap(lifetime, 0, ageToDie, 0, 1));

			// TODO: Move this to a render context! post render isn't good for this
			Rlgl.PushMatrix();
			Rlgl.Translatef(frameState.WindowWidth / 2f, frameState.WindowHeight / 2f, 0);
			Rlgl.Translatef((finalPos.X / 1.68f) - (pos0to1_two * (frameState.WindowWidth * 0.06f / DashGameLevel.GlobalScale)), (-finalPos.Y / 2) - pos, 0);
			Rlgl.Scalef(size, size, size);
			Graphics2D.SetDrawColor(Color, (int)(Color.A * Raymath.Remap((float)lifetime, 0, ageToDie, 1, 0)));
			Graphics2D.DrawText(new(0), Text, Graphics2D.UI_FONT_NAME, 42, TextAlignment.Center, TextAlignment.Center);
			Rlgl.PopMatrix();
		}
	}
}
