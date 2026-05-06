using Nucleus.Types;

namespace CloneDash.Characters;

public interface ICharacterMainMenuInstance : IDisposable
{
	/// <summary>
	/// The base character that created this instance
	/// </summary>
	ICharacterDescriptor GetCharacter();

	/// <summary>
	/// Plays the audio track from the beginning.
	/// </summary>
	void PlayAudio();
	/// <summary>
	/// Stops the audio track.
	/// </summary>
	void StopAudio();
	/// <summary>
	/// Renders the model.
	/// </summary>
	void Render(Vector2F offset = default);

	/// <summary>
	/// Instantiates a random expression. If one is already playing, this will return null.
	/// </summary>
	ICharacterMainMenuExpression? StartExpression();

	/// <summary>
	/// Instantiates the expression played when applied. If one is already playing, it is destroyed, and this plays immediately.
	/// </summary>
	ICharacterMainMenuExpression? StartApplyExpression();
	void Standby();
	void GetPlayingExpression(out ICharacterMainMenuExpression? exp, out double startTime, out double endTime);
}
