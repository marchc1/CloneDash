using CloneDash.Common.Game;

namespace CloneDash.Characters;

public interface ICharacterVictoryInstance
{
	/// <summary>
	/// The base character that created this instance
	/// </summary>
	ICharacterDescriptor GetCharacter();
	/// <summary>
	/// Plays the audio track this character says.
	/// </summary>
	void PlayAudio();
	void Initialize(IGame game);
	/// <summary>
	/// Renders the model.
	/// </summary>
	void Render();
	void Think();
}
