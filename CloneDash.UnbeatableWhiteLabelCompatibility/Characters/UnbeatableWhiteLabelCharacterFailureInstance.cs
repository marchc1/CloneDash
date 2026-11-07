using CloneDash.Characters;
using CloneDash.Common.Game;
using Nucleus;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelCharacterFailureInstance(UnbeatableWhiteLabelCharacterDescriptor descriptor) : ICharacterFailureInstance
{
	const string HURT = "BeatCombatHurt01";

	readonly UnbeatableWhiteLabelBeatModel model = CreateModel();

	public ICharacterDescriptor GetCharacter() => descriptor;

	/// <summary>
	/// Nothing to set up: the model is created and posed when this instance is.
	/// </summary>
	public void Initialize(RhythmGameLevel game) { }

	public void PlayAudio() { }

	/// <summary>
	/// Advances the hurt pose's animation.
	/// </summary>
	public void Think() => model.Update(EngineCore.Level.RealtimeDeltaF);

	public void Render() => model.RenderBeat(default, UnbeatableWhiteLabelCharacterVictoryInstance.SCALE);

	static UnbeatableWhiteLabelBeatModel CreateModel() {
		var model = new UnbeatableWhiteLabelBeatModel();
		model.PlayBeat(HURT);
		model.Update(0);
		return model;
	}
}
