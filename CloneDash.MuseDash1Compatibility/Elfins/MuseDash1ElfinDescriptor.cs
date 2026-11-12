using AssetStudio;
using CloneDash.Characters;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.Models.Runtime;

namespace CloneDash.Elfins;

public class MuseDash1ElfinRetriever() : ModProvider<IElfinDescriptor>("Muse Dash 1", "musedash1"), IElfinProvider
{
	public static string GetID(ElfinConfigData efd) => efd.Prefab;

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var elfin in MuseDash1Compatibility.Elfins)
			if (!elfin.Hide)
				yield return GetID(elfin);
	}

	public override IElfinDescriptor? FindByID(ReadOnlySpan<char> id) {
		if (!MuseDash1Compatibility.Initialized)
			return null;

		foreach (var elfin in MuseDash1Compatibility.Elfins) {
			if (!id.Equals(GetID(elfin), StringComparison.Ordinal)) continue;

			return new MuseDash1ElfinDescriptor(elfin, this);
		}

		return null;
	}
}

public class MuseDash1ElfinDescriptor(ElfinConfigData configData, ICloneDashModProvider provider) : ModDescriptor, IElfinDescriptor
{
	public const string LILITH = "elfin_devil";
	public const string LITTLE_DEVIL_MARIJA = "char_3_evil";
	public const string SILENCER = "elfin_ghost";

	internal readonly ElfinConfigData ConfigData = configData;

	public int Index => ConfigData.Index;

	public override string GetID() => MuseDash1ElfinRetriever.GetID(ConfigData);
	public override ICloneDashModProvider GetProvider() => provider;

	public override string GetName() => ConfigData.Name;
	public ReadOnlySpan<char> GetElfinName() => ConfigData.Name;
	public ReadOnlySpan<char> GetDescription() => ConfigData.Description;
	public ReadOnlySpan<char> GetPerk() => ConfigData.Skill;

	public ITexture? GetThumbnailTexture() {
		var texture = MuseDash1Compatibility.StreamingAssets.FindAssetByName<Texture2D>(ConfigData.Thumb);
		return texture == null ? null : UnityAssetUtils.ConvertTexture(EngineCore.Level, texture);
	}

	public ModelData GetBattleModel(Level level, bool uploadTextures = true) => MuseDash1CharacterDescriptor.PullModelDataFromGameObject(level, ConfigData.Prefab, uploadTextures);
	public float GetBattleScale() => MuseDash1CharacterDescriptor.GetSkeletonScale(ConfigData.Prefab);
	public ModelData GetMainShowModel(Level level) => MuseDash1CharacterDescriptor.PullModelDataFromGameObject(level, ConfigData.MainShow);
	public float GetMainShowScale() => MuseDash1CharacterDescriptor.GetSkeletonScale(ConfigData.MainShow);

	public IElfinMainMenuInstance CreateMainMenu() => new MuseDash1ElfinMainMenuInstance(this);

	public bool SilencesCharacter() => ConfigData.Prefab == SILENCER;

	public void ApplyQuirks(ref MuseDash1GameplayQuirks quirks) {
		MuseDash1GameplayQuirks.ApplyElfinQuirks(GetUniqueID(), ref quirks);

		if (ConfigData.Prefab == LILITH && CharacterMod.Instance.GetActive()?.GetID() == LITTLE_DEVIL_MARIJA) {
			quirks.PerfectHpRevive = 2;
			quirks.MusicNoteAddHp = 2;
		}
	}

	public IGamemodeDescriptor GetGamemode() => ModStores.Get<GamemodeMod>().GetDescriptorByUID(MuseDash1Gamemode.UUID)!;

	public Type GetRuntimeType() => typeof(MuseDash1ElfinInstance);
}
