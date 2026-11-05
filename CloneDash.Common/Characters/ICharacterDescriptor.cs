using CloneDash.Common;
using CloneDash.Common.Compatibility;
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
public interface ICharacterDescriptor : IContentSource
{
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

	ICharacterMainMenuInstance CreateMainMenu();
	ICharacterVictoryInstance CreateVictory();
	ICharacterFailureInstance CreateFailure();
	void ApplyQuirks(ref MuseDash1GameplayQuirks quirks);
}
