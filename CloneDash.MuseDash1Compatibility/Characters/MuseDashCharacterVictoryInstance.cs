using CloneDash.Common.Game;
using Nucleus;
using Nucleus.Models.Runtime;

namespace CloneDash.Characters;

public class MuseDashCharacterVictoryInstance(MuseDash1CharacterDescriptor descriptor) : ICharacterVictoryInstance
{
	public readonly MuseDash1CharacterDescriptor Descriptor = descriptor;
	public ICharacterDescriptor GetCharacter() => Descriptor;

	ModelInstance? model;
	readonly AnimationHandler anims = new();

	public void Initialize(IGame game){
		model = Descriptor.GetVictoryModel(EngineCore.Level).Instantiate();
		anims.SetModel(model);
		anims.SetAnimation(0, "in");
		anims.AddAnimation(0, GetStandby(model), true);
	}

	// Bloodheir uses "Standby"
	private string GetStandby(ModelInstance model) {
		var standby = Descriptor.GetVictoryStandby();
		if (model.Data.FindAnimation(standby) != null)
			return standby;

		foreach (var anim in model.Data.Animations)
			if (string.Equals(anim.Name, standby, StringComparison.OrdinalIgnoreCase))
				return anim.Name;

		return standby;
	}

	public void PlayAudio() {
		// todo
	}

	public void Think() {
		if (model != null) {
			anims?.AddDeltaTime(globals.CurTimeDelta);
			anims?.Apply(model);
		}
	}

	public void Render() {
		model?.Render();
	}
}
