using CloneDash.Characters;
using Nucleus;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace CloneDash.Elfins;

public class MuseDash1ElfinMainMenuInstance : IElfinMainMenuInstance
{
	public const string MAIN_SHOW_STANDBY = "main_show_standby";

	public readonly MuseDash1ElfinDescriptor Descriptor;
	private readonly ModelInstance? Model;
	private readonly AnimationHandler Anims = new();

	public MuseDash1ElfinMainMenuInstance(MuseDash1ElfinDescriptor descriptor) {
		Descriptor = descriptor;
		Model = descriptor.GetMainShowModel(EngineCore.Level).Instantiate();
		Anims.SetModel(Model);

		var standby = Model.Data.FindAnimation(MAIN_SHOW_STANDBY) != null ? MAIN_SHOW_STANDBY : MuseDash1CharacterDescriptor.GetStartingAnimation(descriptor.ConfigData.MainShow);
		if (standby == null || Model.Data.FindAnimation(standby) == null) standby = "standby";
		Anims.SetAnimation(0, standby, true);
	}

	public IElfinDescriptor GetElfin() => Descriptor;

	public void Update() { }

	public void Render(Vector2F offset = default) {
		if (Model == null) return;

		Model.Position = offset;

		Anims.AddDeltaTime(EngineCore.Level.RendertimeDelta);
		Anims.Apply(Model);

		Model.Render();
	}

	public void Dispose() { }
}
