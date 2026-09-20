using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1;

using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.Models.Runtime;

namespace CloneDash.Characters;

/// <summary>
/// An interface for character operations and information.
/// </summary>
public interface ICharacterDescriptor : IUniquelyIdentifiableObject
{
	ReadOnlySpan<char> GetName();
	/// <summary>
	/// Gets the human-friendly character name
	/// </summary>
	ReadOnlySpan<char> GetCharacterName();
	/// <summary>
	/// Gets the human-friendly cosplay name
	/// </summary>
	ReadOnlySpan<char> GetCosplayName();
	/// <summary>
	/// Gets the human-friendly character description
	/// </summary>
	ReadOnlySpan<char> GetDescription();
	ITexture? GetThumbnailTexture();
	/// <summary>
	/// Gets the human-friendly author name
	/// </summary>
	ReadOnlySpan<char> GetAuthor();
	/// <summary>
	/// Gets the human-friendly perk description
	/// </summary>
	ReadOnlySpan<char> GetPerk();

	/// <summary>
	/// Checks if the character supports the provided gamemode.
	/// </summary>
	bool SupportsGamemode(IGamemodeDescriptor gamemodeDescriptor);

	/// <summary>
	/// Some characters have perks to them that adjust certain gamemode behaviors. This will return those parameters.
	/// If the gamemode is not supported, this returns null.
	/// </summary>
	object? GetGamemodeParameters(IGamemodeDescriptor gamemodeDescriptor);

	ICharacterMainMenuInstance CreateMainMenu();
	ICharacterVictoryInstance CreateVictory();
	ICharacterFailureInstance CreateFailure();
	/// <summary>
	/// If the gamemode is not supported, this will return null!
	/// </summary>
	T? CreateInGame<T>(IGame game) where T : ICharacterInGameInstance;

	void ApplyQuirks(ref MuseDash1GameplayQuirks quirks);
}
