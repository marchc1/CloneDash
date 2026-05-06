namespace CloneDash.Characters;

public class MuseDashCharacterFailureInstance(MuseDashCharacterDescriptor descriptor) : ICharacterFailureInstance
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