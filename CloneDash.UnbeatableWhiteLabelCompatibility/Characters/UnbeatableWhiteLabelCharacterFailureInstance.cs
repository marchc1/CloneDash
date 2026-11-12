using CloneDash.Characters;
using CloneDash.Common.Game;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelCharacterFailureInstance(UnbeatableWhiteLabelCharacterDescriptor descriptor) : ICharacterFailureInstance
{
	const string HURT = "BeatCombatHurt01";

	readonly UnbeatableWhiteLabelBeatModel model = CreateModel();

	public ICharacterDescriptor GetCharacter() => descriptor;

	public void Initialize(RhythmGameLevel? game) { }

	public void PlayAudio() { }

	public void Render() => model.RenderBeat(default, UnbeatableWhiteLabelCharacterVictoryInstance.SCALE);

	public void Think() { }

	static UnbeatableWhiteLabelBeatModel CreateModel() {
		var model = new UnbeatableWhiteLabelBeatModel();
		model.PlayBeat(HURT);
		model.Update(0);
		return model;
	}
}
