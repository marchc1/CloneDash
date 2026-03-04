using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;

using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;

namespace CloneDash.Characters;

/// <summary>
/// An interface for character operations and information.
/// </summary>
public interface ICharacterDescriptor
{
	public string GetUniqueID();
	public string GetName();
	public string GetCosplayName();
	public string GetCharacterName();
	public string? GetDescription();
	public ITexture? GetThumbnailTexture();
	public string GetAuthor();
	public string GetPerk();

	public ModelData GetPlayModel(Level level);
	public ModelData GetPlayGhostModel(Level level);
	public ModelData GetMainShowModel(Level level);
	public ModelData GetVictoryModel(Level level);
	public ModelData GetFailModel(Level level);

	public MusicTrack? GetMainShowMusic(Level level);

	public string GetMainShowStandby();
	public string GetVictoryStandby();

	public ICharacterExpression? GetMainShowExpression();
	public ICharacterExpression? GetMainShowApplyExpression();
	public string? GetMainShowInitialExpression();
	public void PlayCharacterAnimation(CharacterAnimationType animationType, MD_SpineActionController animations);
	public void PlayGhostCharacterAnimation(CharacterAnimationType animationType, MD_SpineActionController animations);

	public double GetDefaultHP();
	public string? GetLogicControllerData();
	MD_SpineActionControllerData GetPlayAnimationData();
	MD_SpineActionControllerData GetPlayGhostAnimationData();
}
