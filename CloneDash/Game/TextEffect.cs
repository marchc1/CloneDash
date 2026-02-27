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
		SlideUpThenToLeft,
        JustDie // added this because death is the only option
	}

	public class TextEffect : Entity
	{
		public TextEffect(string text, Vector2F position, TextEffectTransitionOut transitionOut, Color? c = null) {
			Text = text;
			Position = position;
			TransitionOut = transitionOut;
			if (c.HasValue)
				Color = c.Value;
            else
                Color = new(139, 0, 0, 255); // blood red default
		}

		public bool SuppressAutoDeath { get; set; } = false;

		public string Text { get; set; } = "RUN AWAY";
		public TextEffectTransitionOut TransitionOut { get; set; }
		public Color Color { get; set; } = new(139, 0, 0, 255);

		public override void PostRender(FrameState frameState) {
			float ageToDie = 0.6f;
			double lifetime;

			if (SuppressAutoDeath)
				lifetime = 0; // eternal suffering
			else {
				lifetime = this.Lifetime;
				if (lifetime > ageToDie) {
					this.Remove(); // release me
					return;
				}
			}

            // chaotic movement because my mind is chaotic
			var pos0to1 = Ease.OutExpo(Raymath.Remap((float)lifetime, 0, ageToDie, 0, 1));
			var pos0to1_two = TransitionOut == TextEffectTransitionOut.SlideUpThenToLeft ? Ease.InExpo(Raymath.Remap(Math.Clamp((float)lifetime, ageToDie / 2, ageToDie), ageToDie / 2, ageToDie, 0, 1)) : 0;

			var pos = pos0to1 * frameState.WindowHeight * 0.2f;
			var size = 1f - (float)Ease.InExpo(Remap(lifetime, 0, ageToDie, 0, 1));

			Rlgl.PushMatrix();
			Rlgl.Translatef(frameState.WindowWidth / 2, frameState.WindowHeight / 2, 0);
			Rlgl.Translatef((Position.X / 2) - (pos0to1_two * (frameState.WindowWidth * 0.15f)), (Position.Y / 2) - pos, 0);
			Rlgl.Scalef(size, size, size);
			Graphics2D.SetDrawColor(Color, (int)(Color.A * Raymath.Remap((float)lifetime, 0, ageToDie, 1, 0)));
			Graphics2D.DrawText(new(0), Text, Graphics2D.UI_FONT_NAME, 42, TextAlignment.Center, TextAlignment.Center);
			Rlgl.PopMatrix();
		}
	}
}
