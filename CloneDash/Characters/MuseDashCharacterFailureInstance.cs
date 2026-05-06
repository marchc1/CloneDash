namespace CloneDash.Characters;

public class MuseDashCharacterFailureInstance(MuseDash1CharacterDescriptor descriptor) : ICharacterFailureInstance
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