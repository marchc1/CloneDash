namespace CloneDash.Characters;

public interface ICharacterFailureInstance
{
	/// <summary>
	/// The base character that created this instance
	/// </summary>
	ICharacterDescriptor GetCharacter();
	/// <summary>
	/// Plays the audio track this character says.
	/// </summary>
	void PlayAudio();
	/// <summary>
	/// Renders the model.
	/// </summary>
	void Render();
}
