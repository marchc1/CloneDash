using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;

using Nucleus.Common.Graphics;

namespace CloneDash.Elfins;

public interface IElfinDescriptor : IContentSource
{
	ReadOnlySpan<char> GetElfinName();
	ReadOnlySpan<char> GetDescription();
	ITexture? GetThumbnailTexture();
	ReadOnlySpan<char> GetPerk();

	IElfinMainMenuInstance CreateMainMenu();
	bool SilencesCharacter();
	void ApplyQuirks(ref MuseDash1GameplayQuirks quirks);
}
