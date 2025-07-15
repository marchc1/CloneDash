using CloneDash.Settings;

using Nucleus.Audio;
using Nucleus.Engine;
using Nucleus.Models.Runtime;


namespace CloneDash.Characters;

public interface ICharacterExpression
{
	public string GetStartAnimationName();
	public string GetIdleAnimationName();
	public string GetEndAnimationName();
	public void GetSpeech(Level level, out string text, out Sound voice);

	public bool Run(Level level, ModelInstance model, AnimationHandler anims, out string text, out double duration) {
		Nucleus.Models.Runtime.Animation? startAnimation = model.Data.FindAnimation(GetStartAnimationName());
		Nucleus.Models.Runtime.Animation? idleAnimation = model.Data.FindAnimation(GetIdleAnimationName());
		Nucleus.Models.Runtime.Animation? endAnimation = model.Data.FindAnimation(GetEndAnimationName());

		GetSpeech(level, out text, out var voice);
		duration = voice.Duration;

		if (startAnimation == null || idleAnimation == null || endAnimation == null) {
			duration = 0;
			return false;
		}

		anims.SetAnimation(1, GetStartAnimationName());
		anims.AddAnimation(1, GetIdleAnimationName(), loops: true, loopDuration: Math.Max(duration - startAnimation.Duration - endAnimation.Duration, 0.1));
		anims.AddAnimation(1, GetEndAnimationName());
		voice.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		voice.Play();

		return false;
	}
}
