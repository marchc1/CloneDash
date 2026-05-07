namespace CloneDash.Characters;

public class MuseDashCharacterVictoryInstance(MuseDash1CharacterDescriptor descriptor) : ICharacterVictoryInstance
{
	public readonly MuseDash1CharacterDescriptor Descriptor = descriptor;
	public ICharacterDescriptor GetCharacter() => Descriptor;


	public void PlayAudio() {
		throw new NotImplementedException();
	}

	public void Render() {
		throw new NotImplementedException();
	}
}
