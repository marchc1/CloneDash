namespace CloneDash.Characters;

public interface ICharacterInGameInstance
{
	/// <summary>
	/// The base character that created this instance
	/// </summary>
	ICharacterDescriptor GetCharacter();
}