namespace CloneDash.Characters;

public class MuseDashCharacterVictoryInstance(MuseDashCharacterDescriptor descriptor) : ICharacterVictoryInstance
{
	public readonly MuseDashCharacterDescriptor Descriptor = descriptor;
	public ICharacterDescriptor GetCharacter() => Descriptor;


	public void PlayAudio() {
		throw new NotImplementedException();
	}

	public void Render() {
		throw new NotImplementedException();
	}
}
