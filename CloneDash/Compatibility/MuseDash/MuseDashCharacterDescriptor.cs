using AssetStudio;
using CloneDash.Game;
using CloneDash.Modding.Descriptors;
using CloneDash.Modding.Settings;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Engine;
using Nucleus.Models.Runtime;
using System.Collections.Specialized;
using System.Diagnostics;
using Nucleus.Extensions;

namespace CloneDash.Compatibility.MuseDash;

public class MuseDashCharacterExpression : ICharacterExpression
{
	string ICharacterExpression.GetEndAnimationName() {
		throw new NotImplementedException();
	}

	string ICharacterExpression.GetIdleAnimationName() {
		throw new NotImplementedException();
	}

	void ICharacterExpression.GetSpeech(Level level, out string text, out Sound voice) {
		throw new NotImplementedException();
	}

	string ICharacterExpression.GetStartAnimationName() {
		throw new NotImplementedException();
	}
}
public class MuseDashCharacterRetriever : ICharacterRetriever
{
	int ICharacterRetriever.Priority => 0;

	public string GetName(CharacterConfigData cfd) => $"md_{cfd.BGM.Replace("_bgm", "")}";

	IEnumerable<string> ICharacterRetriever.GetAvailableCharacters() {
		foreach (var character in MuseDashCompatibility.Characters) {
			yield return GetName(character);
		}
	}

	ICharacterDescriptor? ICharacterRetriever.GetDescriptorFromName(string name) {
		foreach (var character in MuseDashCompatibility.Characters) {
			if (name != GetName(character)) continue;

			return new MuseDashCharacterDescriptor(character);
		}

		return null;
	}
}

[Nucleus.MarkForStaticConstruction]
public class MuseDashCharacterDescriptor(CharacterConfigData configData) : ICharacterDescriptor
{
	public static ConCommand clonedash_nextmdchar = ConCommand.Register(nameof(clonedash_nextmdchar), (_, _) => {
		var clonedash_character = ((ConVar)ConVar.Get("clonedash_character")!);
		var clonedash_character_value = clonedash_character.GetString();
		ICharacterRetriever retriever = new MuseDashCharacterRetriever();
		bool next = false;
		foreach (var character in retriever.GetAvailableCharacters()) {
			if (character == clonedash_character_value) next = true;
			else if (next) { 
				clonedash_character.SetValue(character);
				Logs.Info($"Selecting '{character}'");
				return;
			}
		}
		Logs.Warn("No more characters available.");
	});

	public string GetName() => $"{configData.Localization["english"].CosName} {configData.Localization["english"].CharacterName}";
	public string? GetDescription() => configData.Localization["english"].Description;
	public string GetAuthor() => "PeroPeroGames";
	public string GetPerk() => $"{configData.Localization["english"].Skill}";
	public double GetDefaultHP() => int.TryParse(configData.DefaultHP, out var hp) ? hp : 250;

	public ModelData GetFailModel(Level level) {
		throw new NotImplementedException();
	}

	public string? GetLogicControllerData() => null;

	public ICharacterExpression? GetMainShowExpression() {
		return null;
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
		int i = 0;
		foreach (var materialBaseObj in materials) {
			var materialBase = (OrderedDictionary)materialBaseObj;
			var materialPathID = (long)materialBase["m_PathID"]!;

			// read material data
			var materialMB = assets.FindAssetByPathID<Material>(materialPathID)!;
			var texPtr = materialMB.m_SavedProperties.m_TexEnvs.First()!.Value.m_Texture;
			if (!texPtr.TryGet(out var tex)) throw new Exception();
			textureIDs[i] = tex.m_PathID;
			i++;
		}

		return MuseDashModelConverter.MD_GetModelData(level, jsonPathID, atlasPathID, textureIDs);
	}

	public static ModelData PullModelDataFromGameObject(Level level, string name) {
		var assets = MuseDashCompatibility.StreamingAssets;

		var mainshowObject = assets.FindAssetByName<GameObject>(name);
		var skeletonMecanim = mainshowObject!.GetMonoBehaviorByScriptName("SkeletonMecanim");
		if (skeletonMecanim == null)
			skeletonMecanim = mainshowObject!.GetMonoBehaviorByScriptName("SkeletonAnimation");

		return PullModelDataFromSkeletonMecanim(level, skeletonMecanim!);
	}

	public ModelData GetMainShowModel(Level level) => PullModelDataFromGameObject(level, configData.MainShow);

	public MusicTrack? GetMainShowMusic(Level level) {
		var audioclip = MuseDashCompatibility.StreamingAssets.FindAssetByName<AudioClip>(configData.BGM);
		if (audioclip == null) return null;

		var audiodata = audioclip.m_AudioData.GetData();

		if (audioclip.m_Type == FMODSoundType.UNKNOWN) {
			FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
			bank.Samples[0].RebuildAsStandardFileFormat(out var at, out var fileExtension);
			return EngineCore.Level.Sounds.LoadMusicFromMemory(at!);
		}

		throw new NotImplementedException();
	}

	public string GetMainShowStandby() => "BgmStandby";

	private Dictionary<CharacterAnimationType, List<string>> anims;
	private void convertAnimations() {
		if (anims != null) return;

		var assets = MuseDashCompatibility.StreamingAssets;

		var mainshowObject = assets.FindAssetByName<GameObject>(configData.BattleShow);
		var actionController = mainshowObject!.GetMonoBehaviorByScriptName("SpineActionController")!;
		var actions = (List<object>)actionController.ToType()["actionData"]!;
		anims = [];
		foreach (var actionObj in actions) {
			var action = (OrderedDictionary)actionObj;

			bool isRandomSequence = (byte)action["isRandomSequence"]! > 0;

			CharacterAnimationType type = (string)action["name"]! switch {
				"char_run" => CharacterAnimationType.Run,
				"in" => CharacterAnimationType.In,
				"char_hurt" => CharacterAnimationType.RoadHurt,
				"char_jump_hurt" => CharacterAnimationType.JumpHurt,
				"char_die" => CharacterAnimationType.Die,
				"char_press" => CharacterAnimationType.Press,
				"char_atk_miss" => CharacterAnimationType.RoadMiss,
				"char_atk_g" => CharacterAnimationType.RoadGreat,
				"char_atk_p" => CharacterAnimationType.RoadPerfect,
				"char_jump" => CharacterAnimationType.Jump,
				"char_jumphit" => CharacterAnimationType.AirPerfect,

				// todo: research this better
				"char_downhit" => CharacterAnimationType.NotApplicable,
				"char_downpress" => CharacterAnimationType.DownPressHit,
				"char_uphit" => CharacterAnimationType.NotApplicable,
				"char_uppress" => CharacterAnimationType.UpPressHit,
				"char_uppress_end" => CharacterAnimationType.AirPressEnd,
				"char_big_press" => CharacterAnimationType.Press,

				// ???
				"char_up_press_s2b" => CharacterAnimationType.NotApplicable,
				"char_up_press_b2s" => CharacterAnimationType.NotApplicable,
				"char_down_press_s2b" => CharacterAnimationType.NotApplicable,
				"char_down_press_b2s" => CharacterAnimationType.NotApplicable,

				"char_bighit" => CharacterAnimationType.Double,
				"char_up_press_s" => CharacterAnimationType.NotApplicable,
				"char_down_press_s" => CharacterAnimationType.NotApplicable,
				"char_uppress_hurt" => CharacterAnimationType.AirPressHurt,
				"char_jumphit_great" => CharacterAnimationType.AirGreat,

				_ => CharacterAnimationType.NotApplicable
			};
			if (type == CharacterAnimationType.NotApplicable) continue;

			if (!anims.TryGetValue(type, out var individualAnims)) {
				individualAnims = [];
				anims[type] = individualAnims;
			}

			var actionIdx = (List<object>)action["actionIdx"]!;
			foreach (var actionId in actionIdx) {
				individualAnims.Add((string)actionId!);
			}
			//individualAnims.Add()
		}

	}

	public string GetPlayAnimation(CharacterAnimationType animationType) {
		convertAnimations();
		return anims[animationType].Random();
	}

	public ModelData GetPlayModel(Level level) => PullModelDataFromGameObject(level, configData.BattleShow);
	public ModelData GetVictoryModel(Level level) => PullModelDataFromGameObject(level, configData.VictoryShow);
	public string GetVictoryStandby() => "standby";
}
