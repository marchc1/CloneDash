using AssetStudio;

using CloneDash.Characters;
using CloneDash.Game;

using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using NAudio.CoreAudioApi;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Compatibility.MuseDash;

public class MuseDashCharacterExpression : ICharacterExpression
{
	private CharacterExpression Expression;
	private string Talk;
	private string AudioName;

	public MuseDashCharacterExpression(CharacterExpression expression, string talk, string audioName) {
		Expression = expression;
		Talk = talk;
		AudioName = audioName;
	}

	string ICharacterExpression.GetEndAnimationName() {
		return $"{Expression.AnimName}_end";
	}

	string ICharacterExpression.GetIdleAnimationName() {
		return $"{Expression.AnimName}_standby";
	}

	void ICharacterExpression.GetSpeech(Level level, out string text, out IAudioClip? voice) {
		text = Talk;
		voice = MuseDashCompatibility.LoadSoundFromName(level, AudioName);
	}

	string ICharacterExpression.GetStartAnimationName() {
		return $"{Expression.AnimName}_start";
	}

	public static MuseDashCharacterExpression From(CharacterConfigData data) {
		int i = Random.Shared.Next(0, data.Expressions.Count);

		var expr = data.Expressions[i];
		var audioNames = expr.AudioNames;
		var audioI = Random.Shared.Next(0, audioNames.Count);

		return new MuseDashCharacterExpression(expr, data.Localization["english"].Expressions[i][audioI], audioNames[audioI]);
	}
}
public class MuseDashCharacterRetriever : ICharacterProvider
{
	int ICharacterProvider.Priority => 0;

	public string GetName(CharacterConfigData cfd) => $"md_{cfd.BGM.Replace("_bgm", "")}";

	IEnumerable<string> ICharacterProvider.GetAvailable() {
		foreach (var character in MuseDashCompatibility.Characters) {
			yield return GetName(character);
		}
	}

	ICharacterDescriptor? ICharacterProvider.FindByName(string name) {
		if (MuseDashCompatibility.Characters == null)
			return null;

		foreach (var character in MuseDashCompatibility.Characters) {
			if (name != GetName(character)) continue;

			return new MuseDashCharacterDescriptor(character, name);
		}

		return null;
	}
}

[Nucleus.MarkForStaticConstruction]
public class MuseDashCharacterDescriptor(CharacterConfigData configData, string name) : ICharacterDescriptor
{
	public string GetUniqueID() => name;

	public static ConCommand nextmdchar = new(nameof(nextmdchar), (_, in _) => {
		var chvar = cvar.FindVar(nameof(CharacterMod.character))!;
		var clonedash_character_value = chvar.GetString();
		ICharacterProvider retriever = new MuseDashCharacterRetriever();
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

	public string GetName() => $"{configData.Localization["english"].CosName} {configData.Localization["english"].CharacterName}";
	public string GetCosplayName() => configData.Localization["english"].CosName;
	public string GetCharacterName() => configData.Localization["english"].CharacterName;
	public ITexture? GetThumbnailTexture() => MuseDashCompatibility.ConvertTexture(EngineCore.Level, MuseDashCompatibility.StreamingAssets.FindAssetByName<Texture2D>(configData.Skins.First().HeadName)!);
	public string? GetDescription() => configData.Localization["english"].Description;
	public string GetAuthor() => configData.Localization["english"].CV;
	public string GetPerk() => $"{configData.Localization["english"].Skill}";
	public double GetDefaultHP() => int.TryParse(configData.DefaultHP, out var hp) ? hp : 250;

	public ModelData GetFailModel(Level level) {
		throw new NotImplementedException();
	}

	public string? GetLogicControllerData() => null;

	public ICharacterExpression? GetMainShowExpression() {
		MuseDashCharacterExpression expression = MuseDashCharacterExpression.From(configData);
		return expression;
	}

	public ICharacterExpression? GetMainShowApplyExpression() {
		// probably need a better way to figure out the folder name
		var assets = MuseDashCompatibility.StreamingAssets;
		var mainShow = assets.FindAssetByName<GameObject>(configData.MainShow);
		var apply = mainShow?.GetMonoBehaviorByScriptName("CharacterApply")?.ToType();
		if (apply is null) return null;

		var animation = (string)apply["characterAnimation"]!;
		var voiceline = (string)apply["characterSound"]!;

		return new MuseDashCharacterExpression(
			configData.Expressions.FirstOrDefault(x => animation.StartsWith(x.AnimName)) ?? configData.Expressions.First(),
			"",
			voiceline
		);
	}

	public string? GetMainShowInitialExpression() => null;

	// I hate this!
	public static ModelData PullModelDataFromSkeletonMecanim(Level level, MonoBehaviour skeletonMecanim) {
		var assets = MuseDashCompatibility.StreamingAssets;

		// This pulls out skeletonDataAsset m_PathID
		// todo: refactor this abomination
		var skeletonDataAsset = (long)((OrderedDictionary)(skeletonMecanim!.ToType())["skeletonDataAsset"]!)["m_PathID"]!;

		// read the skeleton now
		var mainShowAssetMB = assets.FindAssetByPathID<MonoBehaviour>(skeletonDataAsset)!;
		OrderedDictionary mainShowAsset = mainShowAssetMB.ToType();
		// pull out the JSON
		var jsonPathID = (long)((OrderedDictionary)mainShowAsset["skeletonJSON"]!)["m_PathID"]!;
		var atlasAssets = (List<object>)mainShowAsset["atlasAssets"]!;


		var atlasBase = (OrderedDictionary)atlasAssets[0];
		var atlasBaseID = (long)atlasBase["m_PathID"]!;

		// read atlas data
		var atlasMB = assets.FindAssetByPathID<MonoBehaviour>(atlasBaseID)!;
		OrderedDictionary atlasInfo = atlasMB.ToType();

		var atlasPathID = (long)((OrderedDictionary)atlasInfo["atlasFile"]!)["m_PathID"]!;
		var materials = (List<object>)atlasInfo["materials"]!;

		long[] textureIDs = new long[materials.Count];
		Material[] materialsIn = new Material[materials.Count];
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

		return MuseDashModelConverter.MD_GetModelData(level, jsonPathID, atlasPathID, textureIDs, materialsIn);
	}

	public static ModelData PullModelDataFromGameObject(Level level, ReadOnlySpan<char> name) {
		var assets = MuseDashCompatibility.StreamingAssets;

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

	public ModelData GetMainShowModel(Level level) => PullModelDataFromGameObject(level, configData.MainShow);

	public IAudioClip? GetMainShowMusic(Level level) => MuseDashCompatibility.LoadMusicFromName(level, configData.BGM);
	public string GetMainShowStandby() => "BgmStandby";

	private static MD_SpineActionControllerData? black_girl_battle;
	private MD_SpineActionControllerData? anims;
	private MD_SpineActionControllerData? ghostanims;


	[MemberNotNull(nameof(black_girl_battle))]
	private static void convertBaseAnimationData() {
		if (black_girl_battle != null)
			return;

		var data = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>("black_girl_battle")?.GetMonoBehaviorByScriptName("SpineActionController")!;
		ArgumentNullException.ThrowIfNull(data);

		black_girl_battle = new(new(data));
	}

	[MemberNotNull(nameof(anims), nameof(ghostanims))]
	private void convertAnimations() {
		convertBaseAnimationData();
		if (anims != null && ghostanims != null) return;

		var assets = MuseDashCompatibility.StreamingAssets;

		if (anims == null) {
			var mainshowObject = assets.FindAssetByName<GameObject>(configData.GetBattleShow());
			var actionController = mainshowObject!.GetMonoBehaviorByScriptName("SpineActionController")!;
			anims = new(new(actionController), black_girl_battle);
		}

		if (ghostanims == null) {
			var mainshowObject = assets.FindAssetByName<GameObject>(configData.GetBattleShowGhost());
			var actionController = mainshowObject!.GetMonoBehaviorByScriptName("SpineActionController")!;
			ghostanims = new(new(actionController), black_girl_battle);
		}
	}

	public MD_SpineActionControllerData GetPlayAnimationData() {
		convertAnimations();
		return anims;
	}

	public MD_SpineActionControllerData GetPlayGhostAnimationData() {
		convertAnimations();
		return ghostanims;
	}

	public void PlayCharacterAnimation(CharacterAnimationType animationType, MD_SpineActionController handler) {
		convertAnimations();
		playCharacterAnimation(animationType, handler);
	}

	public void PlayGhostCharacterAnimation(CharacterAnimationType animationType, MD_SpineActionController handler) {
		convertAnimations();
		playCharacterAnimation(animationType, handler);
	}

	private void playCharacterAnimation(CharacterAnimationType animationType, MD_SpineActionController handler) {
		string? name = convertAnimationTypeToName(animationType);
		handler.PlaySkeletonAction(new() {
			ActionName = name,
			CustomCompleteEvent = () => playCharacterAnimation(CharacterAnimationType.Run, handler)
		}, false);
	}


	private string? convertAnimationTypeToName(CharacterAnimationType animationType) {
		var name = animationType switch {
			CharacterAnimationType.Run => "char_run",
			CharacterAnimationType.In => "in",
			CharacterAnimationType.Hurt => "char_hurt",
			CharacterAnimationType.JumpHurt => "char_jump_hurt",
			CharacterAnimationType.Die => "char_die",
			CharacterAnimationType.Press => "char_press",
			CharacterAnimationType.AttackMiss => "char_atk_miss",
			CharacterAnimationType.AttackGreat => "char_atk_g",
			CharacterAnimationType.AttackPerfect => "char_atk_p",
			CharacterAnimationType.Jump => "char_jump",
			CharacterAnimationType.JumpHit => "char_jumphit",
			CharacterAnimationType.DownHit => "char_downhit",
			CharacterAnimationType.DownPress => "char_downpress",
			CharacterAnimationType.UpHit => "char_uphit",
			CharacterAnimationType.UpPressStart => "char_uppress_start",
			CharacterAnimationType.UpPress => "char_uppress",
			CharacterAnimationType.UpPressEnd => "char_uppress_end",
			CharacterAnimationType.BigPress => "char_big_press",
			CharacterAnimationType.UpPressS2B => "char_up_press_s2b",
			CharacterAnimationType.DownPressS2B => "char_down_press_s2b",
			CharacterAnimationType.UpPressB2S => "char_up_press_b2s",
			CharacterAnimationType.DownPressB2S => "char_down_press_b2s",
			CharacterAnimationType.BigHit => "char_bighit",
			CharacterAnimationType.UpPressSmall => "char_up_press_s",
			CharacterAnimationType.DownPressSmall => "char_down_press_s",
			CharacterAnimationType.UpPressHurt => "char_uppress_hurt",
			CharacterAnimationType.JumpHitGreat => "char_jumphit_great",
			_ => null
		};
		return name;
	}

	public ModelData GetPlayModel(Level level) => PullModelDataFromGameObject(level, configData.GetBattleShow());
	public ModelData GetPlayGhostModel(Level level) => PullModelDataFromGameObject(level, configData.GetBattleShowGhost());
	public ModelData GetVictoryModel(Level level) => PullModelDataFromGameObject(level, configData.VictoryShow);
	public string GetVictoryStandby() => "standby";
}
