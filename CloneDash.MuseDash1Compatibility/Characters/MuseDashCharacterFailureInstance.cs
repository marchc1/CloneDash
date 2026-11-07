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

	public void Initialize(IGame game) {
		model = Descriptor.GetFailModel(EngineCore.Level).Instantiate();
		anims.SetModel(model);
		anims.SetAnimation(0, Descriptor.GetFailStandby(), true);
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