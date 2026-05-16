using AssetStudio;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.Models.Runtime;
using Nucleus.Util;
using System.Collections.Frozen;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Characters;

public class MuseDash1CharacterExpression : ICharacterMainMenuExpression
{
	private CharacterExpression Expression;
	private string Talk;
	private string AudioName;

	public MuseDash1CharacterExpression(CharacterExpression expression, string talk, string audioName) {
		Expression = expression;
		Talk = talk;
		AudioName = audioName;
	}

	string ICharacterMainMenuExpression.GetEndAnimationName() {
		return $"{Expression.AnimName}_end";
	}

	string ICharacterMainMenuExpression.GetIdleAnimationName() {
		return $"{Expression.AnimName}_standby";
	}

	void ICharacterMainMenuExpression.GetSpeech(Level level, out string text, out IAudioClip? voice) {
		text = Talk;
		voice = MuseDash1Compatibility.LoadSoundFromName(level, AudioName);
	}

	string ICharacterMainMenuExpression.GetStartAnimationName() {
		return $"{Expression.AnimName}_start";
	}

	public static MuseDash1CharacterExpression From(CharacterConfigData data) {
		int i = Random.Shared.Next(0, data.Expressions.Count);

		var expr = data.Expressions[i];
		var audioNames = expr.AudioNames;
		var audioI = Random.Shared.Next(0, audioNames.Count);

		return new MuseDash1CharacterExpression(expr, data.Localization["english"].Expressions[i][audioI], audioNames[audioI]);
	}
}

public class MuseDash1CharacterRetriever : ICharacterProvider
{
	int ICharacterProvider.Priority => 0;

	public string GetName(CharacterConfigData cfd) => $"character/musedash1/{cfd.BGM.Replace("_bgm", "")}";

	IEnumerable<string> ICharacterProvider.GetAvailable() {
		foreach (var character in MuseDash1Compatibility.Characters) {
			yield return GetName(character);
		}
	}

	ICharacterDescriptor? ICharacterProvider.FindByName(string name) {
		if (MuseDash1Compatibility.Characters == null)
			return null;

		foreach (var character in MuseDash1Compatibility.Characters) {
			if (name != GetName(character)) continue;

			return new MuseDash1CharacterDescriptor(character, name);
		}

		return null;
	}
}

[Nucleus.MarkForStaticConstruction]
public class MuseDash1CharacterDescriptor(CharacterConfigData configData, string name) : ICharacterDescriptor
{
	internal readonly CharacterConfigData ConfigData = configData;

	public ReadOnlySpan<char> GetUUID() => name;

	public static ConCommand nextmdchar = new(nameof(nextmdchar), (_, in _) => {
		var chvar = cvar.FindVar(nameof(CharacterMod.character))!;
		var clonedash_character_value = chvar.GetString();
		ICharacterProvider retriever = new MuseDash1CharacterRetriever();
		bool next = false;
		foreach (var character in retriever.GetAvailable()) {
			if (character == clonedash_character_value) next = true;
			else if (next) {
				chvar.SetValue(character);
				Logs.Info($"Selecting '{character}'");
				return;
			}
		}
		Logs.Warn("No more characters available.");
	});

	static ReadOnlySpan<char> LocalizationLookup(
		CharacterConfigData configData,
		in HumanLanguage desiredLanguage,
		out HumanLanguage returnedLanguage,
		Func<CharacterLocalizationData, string> fetchLocalization,
		Func<CharacterConfigData, string> fetchFallback) {

		string? key = desiredLanguage.Culture.TwoLetterISOLanguageName switch {
			"en" => "english",
			_ => null
		};

		if (key != null && configData.Localization.TryGetValue(key, out var localization)) {
			returnedLanguage = desiredLanguage;
			return fetchLocalization(localization);
		}

		if (key != "english" && configData.Localization.TryGetValue("english", out localization)) {
			returnedLanguage = HumanLanguage.English;
			return fetchLocalization(localization);
		}

		returnedLanguage = HumanLanguage.Any;
		return fetchFallback(configData);
	}

	// cosplay name and character name clash over returnedLanguage, this should be fixed...
	public ReadOnlySpan<char> GetName(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage)
		=> $"{GetCosplayName(desiredLanguage, out returnedLanguage)} {GetCharacterName(desiredLanguage, out returnedLanguage)}";

	public ReadOnlySpan<char> GetCosplayName(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage)
		=> LocalizationLookup(ConfigData, desiredLanguage, out returnedLanguage, x => x.CosName, x => x.CosName);

	public ReadOnlySpan<char> GetCharacterName(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage)
		=> LocalizationLookup(ConfigData, desiredLanguage, out returnedLanguage, x => x.CharacterName, x => x.CharacterName);

	public ITexture? GetThumbnailTexture() => MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<Texture2D>(ConfigData.Skins.First().HeadName)!);

	public ReadOnlySpan<char> GetDescription(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage)
		=> LocalizationLookup(ConfigData, desiredLanguage, out returnedLanguage, x => x.Description, x => x.Description);
	public ReadOnlySpan<char> GetAuthor(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage)
		=> LocalizationLookup(ConfigData, desiredLanguage, out returnedLanguage, x => x.CV, x => x.Cv);
	public ReadOnlySpan<char> GetPerk(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage)
		=> LocalizationLookup(ConfigData, desiredLanguage, out returnedLanguage, x => x.Skill, x => x.Skill);

	public ICharacterMainMenuExpression? GetMainShowExpression() {
		MuseDash1CharacterExpression expression = MuseDash1CharacterExpression.From(ConfigData);
		return expression;
	}

	public ICharacterMainMenuExpression? GetMainShowApplyExpression() {
		// probably need a better way to figure out the folder name
		var assets = MuseDash1Compatibility.StreamingAssets;
		var mainShow = assets.FindAssetByName<GameObject>(ConfigData.MainShow);
		var apply = mainShow?.GetMonoBehaviorByScriptName("CharacterApply")?.ToType();
		if (apply is null) return null;

		var animation = (string)apply["characterAnimation"]!;
		var voiceline = (string)apply["characterSound"]!;

		var exp = ConfigData.Expressions.FindIndex(x => animation.StartsWith(x.AnimName));
		if (exp == -1)
			return null;

		return new MuseDash1CharacterExpression(
			ConfigData.Expressions[exp],
			string.Empty,
			voiceline
		);
	}

	public void ApplyQuirks(ref MuseDash1GameplayQuirks quirks){
		MuseDash1GameplayQuirks.ApplyCharacterQuirks(name, ref quirks);
	}

	// I hate this!
	public static ModelData PullModelDataFromSkeletonMecanim(Level level, MonoBehaviour skeletonMecanim) {
		var assets = MuseDash1Compatibility.StreamingAssets;

		// This pulls out skeletonDataAsset m_PathID
		// todo: refactor this abomination
		var skeletonDataAsset = (long)((OrderedDictionary)(skeletonMecanim!.ToType())["skeletonDataAsset"]!)["m_PathID"]!;

		// read the skeleton now
		var mainShowAssetMB = assets.FindAssetByPathID<MonoBehaviour>(skeletonDataAsset)!;
		OrderedDictionary mainShowAsset = mainShowAssetMB.ToType();
		// pull out the JSON
		var jsonPathID = (long)((OrderedDictionary)mainShowAsset["skeletonJSON"]!)["m_PathID"]!;
		var atlasAssets = (object[])mainShowAsset["atlasAssets"]!;


		var atlasBase = (OrderedDictionary)atlasAssets[0];
		var atlasBaseID = (long)atlasBase["m_PathID"]!;

		// read atlas data
		var atlasMB = assets.FindAssetByPathID<MonoBehaviour>(atlasBaseID)!;
		OrderedDictionary atlasInfo = atlasMB.ToType();

		var atlasPathID = (long)((OrderedDictionary)atlasInfo["atlasFile"]!)["m_PathID"]!;
		var materials = (object[])atlasInfo["materials"]!;

		long[] textureIDs = new long[materials.Length];
		Material[] materialsIn = new Material[materials.Length];
		int i = 0;
		foreach (var materialBaseObj in materials) {
			var materialBase = (OrderedDictionary)materialBaseObj;
			var materialPathID = (long)materialBase["m_PathID"]!;

			// read material data
			var materialMB = assets.FindAssetByPathID<Material>(materialPathID)!;
			var texPtr = materialMB.m_SavedProperties.m_TexEnvs.First()!.Value.m_Texture;
			if (!texPtr.TryGet(out var tex)) throw new Exception();
			textureIDs[i] = tex.m_PathID;
			materialsIn[i] = materialMB;
			i++;
		}

		return MuseDash1ModelConverter.MD_GetModelData(level, jsonPathID, atlasPathID, textureIDs, materialsIn);
	}

	public static ModelData PullModelDataFromGameObject(Level level, ReadOnlySpan<char> name) {
		var assets = MuseDash1Compatibility.StreamingAssets;

		var mainshowObject = assets.FindAssetByName<GameObject>(name);
		var skeletonMecanim = mainshowObject!.GetMonoBehaviorByScriptName("SkeletonMecanim");
		if (skeletonMecanim == null)
			skeletonMecanim = mainshowObject!.GetMonoBehaviorByScriptName("SkeletonAnimation");
		if (skeletonMecanim == null)
			skeletonMecanim = mainshowObject!.GetMonoBehaviorByScriptName("SkeletonGraphic");
		if (skeletonMecanim == null) {
			// OK time to go through the depths of hell for a victory model
			var rectTransform = mainshowObject.GetFirstComponent<RectTransform>()!;
			rectTransform.m_Children[0].TryGet(out rectTransform!);
			rectTransform.m_GameObject.TryGet(out mainshowObject);
			skeletonMecanim = mainshowObject.GetMonoBehaviorByScriptName("SkeletonGraphic");
		}

		return PullModelDataFromSkeletonMecanim(level, skeletonMecanim!);
	}

	public ModelData GetMainShowModel(Level level) => PullModelDataFromGameObject(level, ConfigData.MainShow);

	public IAudioClip? GetMainShowMusic(Level level) => MuseDash1Compatibility.LoadMusicFromName(level, ConfigData.BGM);
	public string GetMainShowStandby() => "BgmStandby";

	private static MD1_SpineActionControllerData? black_girl_battle;
	private MD1_SpineActionControllerData? anims;
	private MD1_SpineActionControllerData? ghostanims;


	[MemberNotNull(nameof(black_girl_battle))]
	private static void convertBaseAnimationData() {
		if (black_girl_battle != null)
			return;

		var data = MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>("black_girl_battle")?.GetMonoBehaviorByScriptName("SpineActionController")!;
		ArgumentNullException.ThrowIfNull(data);

		black_girl_battle = new(new(data));
	}

	[MemberNotNull(nameof(anims), nameof(ghostanims))]
	private void convertAnimations() {
		convertBaseAnimationData();
		if (anims != null && ghostanims != null) return;

		var assets = MuseDash1Compatibility.StreamingAssets;

		if (anims == null) {
			var mainshowObject = assets.FindAssetByName<GameObject>(ConfigData.GetBattleShow());
			var actionController = mainshowObject!.GetMonoBehaviorByScriptName("SpineActionController")!;
			anims = new(new(actionController), black_girl_battle);
		}

		if (ghostanims == null) {
			var mainshowObject = assets.FindAssetByName<GameObject>(ConfigData.GetBattleShowGhost());
			var actionController = mainshowObject!.GetMonoBehaviorByScriptName("SpineActionController")!;
			ghostanims = new(new(actionController), black_girl_battle);
		}
	}

	public MD1_SpineActionControllerData GetPlayAnimationData() {
		convertAnimations();
		return anims;
	}

	public MD1_SpineActionControllerData GetPlayGhostAnimationData() {
		convertAnimations();
		return ghostanims;
	}

	public void PlayCharacterAnimation(CharacterAnimationType animationType, MD1_SpineActionController handler) {
		convertAnimations();
		playCharacterAnimation(animationType, handler);
	}

	public void PlayGhostCharacterAnimation(CharacterAnimationType animationType, MD1_SpineActionController handler) {
		convertAnimations();
		playCharacterAnimation(animationType, handler);
	}

	private void playCharacterAnimation(CharacterAnimationType animationType, MD1_SpineActionController handler) {
		string? name = convertAnimationTypeToName(animationType);
		handler.PlaySkeletonAction(new() {
			ActionName = name,
			CustomCompleteEvent = (a) => {
				if (!a.IsEndLoop)
					playCharacterAnimation(CharacterAnimationType.Run, handler);
			}
		}, false);
	}


	private string? convertAnimationTypeToName(CharacterAnimationType animationType) {
		var name = animationType switch {
			CharacterAnimationType.Run => ActionKeys.RUN,
			CharacterAnimationType.In => ActionKeys.COMEIN,
			CharacterAnimationType.Hurt => ActionKeys.HURT,
			CharacterAnimationType.JumpHurt => ActionKeys.JUMP_HURT,
			CharacterAnimationType.Die => ActionKeys.CHAR_DEAD,
			CharacterAnimationType.Press => ActionKeys.PRESS,
			CharacterAnimationType.AttackMiss => ActionKeys.ATTACK_MISS,
			CharacterAnimationType.AttackGreat => ActionKeys.ATTACK_GREAT,
			CharacterAnimationType.AttackPerfect => ActionKeys.ATTACK_PERFECT,
			CharacterAnimationType.Jump => ActionKeys.JUMP,
			CharacterAnimationType.JumpHit => ActionKeys.JUMP_ATTACK,
			CharacterAnimationType.DownHit => ActionKeys.JUMP_DOWN_ATTACK,
			CharacterAnimationType.DownPress => ActionKeys.JUMP_DOWN_PRESS,
			CharacterAnimationType.UpHit => ActionKeys.JUMP_ATTACK_UP,
			CharacterAnimationType.UpPressStart => ActionKeys.AIR_PRESS_START,
			CharacterAnimationType.UpPress => ActionKeys.AIR_PRESSING,
			CharacterAnimationType.UpPressEnd => ActionKeys.AIR_PRESS_END,
			CharacterAnimationType.BigPress => ActionKeys.PRESS_BIG,
			CharacterAnimationType.PressGroundToBig => ActionKeys.PRESS_GROUND_TO_BIG,
			CharacterAnimationType.PressAirToBig => ActionKeys.PRESS_AIR_TO_BIG,
			CharacterAnimationType.PressBigToGround => ActionKeys.PRESS_BIG_TO_GROUND,
			CharacterAnimationType.PressBigToAir => ActionKeys.PRESS_BIG_TO_AIR,
			CharacterAnimationType.PressHitToGround => ActionKeys.PRESS_HIT_TO_GROUND,
			CharacterAnimationType.PressHitToAir => ActionKeys.PRESS_HIT_TO_AIR,
			CharacterAnimationType.BigHit => ActionKeys.ATTACK_DOUBLE,
			CharacterAnimationType.UpPressHurt => ActionKeys.AIR_PRESS_HURT,
			CharacterAnimationType.JumpHitGreat => ActionKeys.JUMP_ATTACK_GREAT,
			_ => null
		};
		return name;
	}

	public ModelData GetPlayModel(Level level) => PullModelDataFromGameObject(level, ConfigData.GetBattleShow());
	public ModelData GetPlayGhostModel(Level level) => PullModelDataFromGameObject(level, ConfigData.GetBattleShowGhost());
	public ModelData GetVictoryModel(Level level) => PullModelDataFromGameObject(level, ConfigData.VictoryShow);
	public string GetVictoryStandby() => "standby";

	public bool SupportsGamemode(IGamemodeDescriptor gamemodeDescriptor) {
		return false;
	}

	public object? GetGamemodeParameters(IGamemodeDescriptor gamemodeDescriptor) {
		throw new NotImplementedException();
	}

	public ICharacterMainMenuInstance CreateMainMenu() => new MuseDashCharacterMainMenuInstance(this);
	public ICharacterVictoryInstance CreateVictory() => new MuseDashCharacterVictoryInstance(this);
	public ICharacterFailureInstance CreateFailure() => new MuseDashCharacterFailureInstance(this);

	public T? CreateInGame<T>(IGame game) where T : ICharacterInGameInstance {
		switch (game.GetGamemode().GetUUID()) {
			case MuseDash1Gamemode.UUID: return (T)(object)(new MuseDash1CharacterInstance(this, (MuseDash1Game)game));
			case MuseDash1TouhouGamemode.UUID: return (T)(object)(new MuseDash1CharacterInstance(this, (MuseDash1Game)game));
		}

		return default; // not supported
	}
}
