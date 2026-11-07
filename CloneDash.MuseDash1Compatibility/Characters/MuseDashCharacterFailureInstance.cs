using CloneDash.Common.Game;
using Nucleus;
using Nucleus.Models.Runtime;

namespace CloneDash.Characters;

public class MuseDashCharacterFailureInstance(MuseDash1CharacterDescriptor descriptor) : ICharacterFailureInstance
{
	public readonly MuseDash1CharacterDescriptor Descriptor = descriptor;
	public ICharacterDescriptor GetCharacter() => Descriptor;

	ModelInstance? model;
	readonly AnimationHandler anims = new();

	public void Initialize(RhythmGameLevel game) {
		model = Descriptor.GetFailModel(EngineCore.Level).Instantiate();
		float scale = Descriptor.GetFailModelScale() * 100;
		model.Scale = new(scale, scale);
		anims.SetModel(model);
		anims.SetAnimation(0, GetStandby(model), true);
	}

	// The fail prefabs' SkeletonGraphic starts on a looping "standby" (there's no "in"); match case like the victory model does
	private string GetStandby(ModelInstance model) {
		var standby = Descriptor.GetFailStandby();
		if (model.Data.FindAnimation(standby) != null)
			return standby;

		foreach (var anim in model.Data.Animations)
			if (string.Equals(anim.Name, standby, StringComparison.OrdinalIgnoreCase))
				return anim.Name;

		return standby;
	}

	public void PlayAudio() {
		// Muse Dash's fail screen has no character voice line
	}

	public void Think() {
		if (model != null) {
			anims.AddDeltaTime(globals.CurTimeDelta);
			anims.Apply(model);
		}
	}

	public void Render() {
		model?.Render();
	}
}
