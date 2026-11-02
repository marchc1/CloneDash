using AssetStudio;
using CloneDash.Common;
using CloneDash.Common.Compatibility;
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
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CloneDash.Characters;

public class MuseDash1CharacterExpression : ICharacterMainMenuExpression
{
	private string AnimName;
	private string Talk;
	private string AudioName;

	public MuseDash1CharacterExpression(string animName, string talk, string audioName) {
		AnimName = animName;
		Talk = talk;
		AudioName = audioName;
	}

	string ICharacterMainMenuExpression.GetEndAnimationName() {
		return $"{AnimName}_end";
	}

	string ICharacterMainMenuExpression.GetIdleAnimationName() {
		return $"{AnimName}_standby";
	}

	void ICharacterMainMenuExpression.GetSpeech(Level level, out string text, out IAudioClip? voice) {
		text = Talk;
		voice = MuseDash1Compatibility.LoadSoundFromName(level, AudioName);
	}

	string ICharacterMainMenuExpression.GetStartAnimationName() {
		return $"{AnimName}_start";
	}

	// Weighted like ExpressionContainer.RefreshRandom. Weight 0 never plays (the "t" transform voicelines use this)
	public static MuseDash1CharacterExpression? From(CharacterConfigData data, int form) {
		var expressions = data.Expressions;

		double total = 0;
		foreach (var expr in expressions)
			if (expr.Weight > 0 && expr.IsAvailableForForm(form))
				total += expr.Weight;

		if (total <= 0)
			return null;

		double roll = Random.Shared.NextDouble() * total;
		int i = -1;
		for (int j = 0; j < expressions.Count; j++) {
			var expr = expressions[j];
			if (expr.Weight <= 0 || !expr.IsAvailableForForm(form)) continue;

			i = j;
			roll -= expr.Weight;
			if (roll < 0) break;
		}

		var picked = expressions[i];
		var audioNames = picked.AudioNames;
		var audioI = Random.Shared.Next(0, audioNames.Count);

		return new MuseDash1CharacterExpression(picked.AnimName, data.Localization[localize.GetCurrentCulture()].Expressions[i][audioI], audioNames[audioI]);
	}
}

public class MuseDash1CharacterRetriever() : ModProvider<ICharacterDescriptor>("Muse Dash 1", "musedash1"), ICharacterProvider
{
	public static string GetID(CharacterConfigData cfd) => cfd.BGM.Replace("_bgm", "");

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var character in MuseDash1Compatibility.Characters)
			yield return GetID(character);
	}

	public override ICharacterDescriptor? FindByID(ReadOnlySpan<char> id) {
		if (MuseDash1Compatibility.Characters == null)
			return null;

		foreach (var character in MuseDash1Compatibility.Characters) {
			if (!id.Equals(GetID(character), StringComparison.Ordinal)) continue;

			return new MuseDash1CharacterDescriptor(character, this);
		}

		return null;
	}
}

[Nucleus.MarkForStaticConstruction]
public class MuseDash1CharacterDescriptor(CharacterConfigData configData, ICloneDashModProvider provider) : ModDescriptor, ICharacterDescriptor
{
	internal readonly CharacterConfigData ConfigData = configData;

	public override string GetID() => MuseDash1CharacterRetriever.GetID(ConfigData);
	public override ICloneDashModProvider GetProvider() => provider;

	public static ConCommand nextmdchar = new(nameof(nextmdchar), (_, in _) => {
		var store = CharacterMod.Instance;
		var provider = store.GetProviderByID("musedash1");
		if (provider == null) return;

		var chvar = CharacterMod.character;
		var clonedash_character_value = chvar.GetString();
		bool next = false;
		foreach (var id in provider.GetAvailableIDs()) {
			var character = store.BuildUID(provider, id);
			if (clonedash_character_value.Equals(character, StringComparison.Ordinal)) next = true;
			else if (next) {
				chvar.SetValue(character);
				Logs.Info($"Selecting '{character}'");
				return;
			}
		}
		Logs.Warn("No more characters available.");
	});

	public static ConVar md_rinthief = new(nameof(md_rinthief), "0", FCvar.Saved, "Phantom Thief Rin's form", 0, 1);
	public static ConVar md_bloodheirbig = new(nameof(md_bloodheirbig), "0", FCvar.Saved, "Nocturne Bloodheir Buro's form", 0, 1);

	private sealed record CharacterForm(string Standby, string? ExpressingStandby, string ApplyAnimKey, string ApplySoundKey, string? Bgm);

	private bool formsResolved;
	private CharacterForm[]? forms;
	private ConVar? formVar;

	private static string mbString(OrderedDictionary mb, string key, string fallback)
		=> mb[key] is string { Length: > 0 } value ? value : fallback;

	private CharacterForm[]? GetForms() {
		if (formsResolved)
			return forms;

		formsResolved = true;
		var mainShow = MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(ConfigData.MainShow);
		if (mainShow == null)
			return null;

		var thief = mainShow.GetMonoBehaviorByScriptName("ThiefTransformGenerator")?.ToType();
		if (thief != null) {
			var police = mbString(thief, "m_PoliceStandbyAnim", "standby_police");
			var kaito = mbString(thief, "m_KaitoStandbyAnim", "standby_thief");

			formVar = md_rinthief;
			forms = [
				new(police, $"{police}_B", "m_PoliceApplyAnimName", "m_PoliceApplySoundName", null),
				new(kaito, $"{kaito}_B", "m_ThiefApplyAnimName", "m_ThiefApplySoundName", $"{ConfigData.BGM}_mask"),
			];
		}
		else if (mainShow.GetMonoBehaviorByScriptName("BloodheirTransformGenerator") != null) {
			formVar = md_bloodheirbig;
			forms = [
				new("standby_A", null, "m_SmallApplyAnimName", "m_SmallApplySoundName", null),
				new("standby_B", null, "m_BigApplyAnimName", "m_BigApplySoundName", null),
			];
		}

		return forms;
	}

	public int GetForm() => GetForms() == null ? 0 : formVar!.GetBool() ? 2 : 1;

	private CharacterForm? GetActiveForm() {
		int form = GetForm();
		return form == 0 ? null : forms![form - 1];
	}

	// cosplay name and character name clash over returnedLanguage, this should be fixed...
	public override string GetName() => $"{GetCosplayName()} {GetCharacterName()}";
	public ReadOnlySpan<char> GetCosplayName() => ConfigData.CosName;
	public ReadOnlySpan<char> GetCharacterName() => ConfigData.CharacterName;
	public ITexture? GetThumbnailTexture() => MuseDash1Compatibility.ConvertTexture(EngineCore.Level, MuseDash1Compatibility.StreamingAssets.FindAssetByName<Texture2D>(ConfigData.Skins.First().HeadName)!);
	public ReadOnlySpan<char> GetDescription() => ConfigData.Description;
	public ReadOnlySpan<char> GetAuthor() => ConfigData.Cv;
	public ReadOnlySpan<char> GetPerk() => ConfigData.Skill;

	public ICharacterMainMenuExpression? GetMainShowExpression() => MuseDash1CharacterExpression.From(ConfigData, GetForm());

	// Thief/Bloodheir/Pirate/Spectre all subclass CharacterApply
	private static MonoBehaviour? FindCharacterApply(GameObject obj) {
		foreach (var compPtr in obj.m_Components) {
			if (!compPtr.TryGet(out var comp)) continue;
			if (comp is not MonoBehaviour mb) continue;
			if (!mb.m_Script.TryGet(out var script)) continue;

			if (script.m_Name.EndsWith("CharacterApply"))
				return mb;
		}

		return null;
	}

	public ICharacterMainMenuExpression? GetMainShowApplyExpression() {
		// probably need a better way to figure out the folder name
		var assets = MuseDash1Compatibility.StreamingAssets;
		var mainShow = assets.FindAssetByName<GameObject>(ConfigData.MainShow);
		if (mainShow == null) return null;

		var apply = FindCharacterApply(mainShow)?.ToType();
		if (apply is null) return null;

		var form = GetActiveForm();
		string animKey = form?.ApplyAnimKey ?? "characterAnimation";
		string soundKey = form?.ApplySoundKey ?? "characterSound";

		if (apply[animKey] is not string animation || animation.Length == 0)
			return null;

		if (animation.EndsWith("_start"))
			animation = animation[..^"_start".Length];

		return new MuseDash1CharacterExpression(
			animation,
			string.Empty,
			(string)apply[soundKey]!
		);
	}

	public void ApplyQuirks(ref MuseDash1GameplayQuirks quirks) {
		MuseDash1GameplayQuirks.ApplyCharacterQuirks(GetUniqueID(), ref quirks);
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

	public IAudioClip? GetMainShowMusic(Level level) => MuseDash1Compatibility.LoadMusicFromName(level, GetActiveForm()?.Bgm ?? ConfigData.BGM);

	// The looping body animation on the main show.
	// NOTE: Thief Rin swaps to a _B variant while an expression plays (ThiefCharacterApply.PreCharacterApply)!!!
	// This probably deserves its own interface to handle main-show behavior separately... kinda getting crowded in here frankly
	public string GetMainShowStandby(bool expressing = false) {
		var form = GetActiveForm();
		if (form == null)
			return "BgmStandby";

		return expressing && form.ExpressingStandby != null ? form.ExpressingStandby : form.Standby;
	}

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
