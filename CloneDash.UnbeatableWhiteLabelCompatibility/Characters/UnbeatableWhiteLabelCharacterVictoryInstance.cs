using CloneDash.Characters;
using CloneDash.Common.Game;
using Nucleus;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelCharacterVictoryInstance(UnbeatableWhiteLabelCharacterDescriptor descriptor) : ICharacterVictoryInstance
{
	public const float SCALE = 550;

	UnbeatableWhiteLabelBeatModel? model;

	public ICharacterDescriptor GetCharacter() => descriptor;

	public void Initialize(RhythmGameLevel? game) {
		model = new UnbeatableWhiteLabelBeatModel();
		model.PlayBeat(UnbeatableWhiteLabelCharacterMainMenuInstance.STANDBY);
	}

	public void PlayAudio() { }

	public void Render() => model?.RenderBeat(default, SCALE);

	public void Think() => model?.Update(EngineCore.Level.RealtimeDeltaF);
}
