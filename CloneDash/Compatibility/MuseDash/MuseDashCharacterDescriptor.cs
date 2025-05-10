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
		foreach(var character in MuseDashCompatibility.Characters) {
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

public class MuseDashCharacterDescriptor(CharacterConfigData configData) : ICharacterDescriptor
{
	public string GetName() => $"{configData.Localization["english"].CosName} {configData.Localization["english"].CharacterName}";
	public string? GetDescription() => configData.Localization["english"].Description;
	public string GetAuthor() => "PeroPeroGames";
	public string GetPerk() => $"{configData.Localization["english"].Skill}";
	public double GetDefaultHP() => int.TryParse(configData.DefaultHP, out var hp) ? hp : 250;

	public ModelData GetFailModel(Level level) {
		throw new NotImplementedException();
	}

	public string? GetLogicControllerData() {
		throw new NotImplementedException();
	}

	public ICharacterExpression? GetMainShowExpression() {
		throw new NotImplementedException();
	}

	public string? GetMainShowInitialExpression() {
		throw new NotImplementedException();
	}

	public ModelData GetMainShowModel(Level level) {
		var assets = MuseDashCompatibility.StreamingAssets;
		
		var mainshowObject = assets.FindAssetByName<GameObject>(configData.MainShow);
		var skeletonMecanim = mainshowObject!.GetMonoBehaviorByScriptName("SkeletonMecanim");

		// This pulls out skeletonDataAsset m_PathID
		// todo: refactor this abomination
		var skeletonDataAsset = (long)((OrderedDictionary)(skeletonMecanim!.ToType())["skeletonDataAsset"]!)["m_PathID"]!;

		// read the skeleton now
		var mainShowAssetMB = assets.FindAssetByPathID<MonoBehaviour>(skeletonDataAsset)!;
		OrderedDictionary mainShowAsset = mainShowAssetMB.ToType();
		// pull out the JSON
		var jsonPathID = (long)((OrderedDictionary)mainShowAsset["skeletonJSON"]!)["m_PathID"]!;
		var atlasAssets = (List<object>)mainShowAsset["atlasAssets"]!;
		Debug.Assert(atlasAssets.Count == 1); // if not; it will definitely fail to convert
		var atlasBase = (OrderedDictionary)atlasAssets[0];
		var atlasBaseID = (long)atlasBase["m_PathID"]!;

		// read atlas data
		var atlasMB = assets.FindAssetByPathID<MonoBehaviour>(atlasBaseID)!;
		OrderedDictionary atlasInfo = atlasMB.ToType();

		var atlasPathID = (long)((OrderedDictionary)atlasInfo["atlasFile"]!)["m_PathID"]!;
		var materials = (List<object>)atlasInfo["materials"]!;
		Debug.Assert(materials.Count == 1); // if not; it will definitely fail to convert
		var materialBase = (OrderedDictionary)materials[0];
		var materialPathID = (long)materialBase["m_PathID"]!;

		// read material data
		var materialMB = assets.FindAssetByPathID<Material>(materialPathID)!;
		var texPtr = materialMB.m_SavedProperties.m_TexEnvs.First()!.Value.m_Texture;
		if (!texPtr.TryGet(out var tex)) throw new Exception();

		return MuseDashModelConverter.MD_GetModelData(level, jsonPathID, atlasPathID, tex.m_PathID);
	}

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

	public string GetPlayAnimation(CharacterAnimationType animationType) {
		throw new NotImplementedException();
	}

	public ModelData GetPlayModel(Level level) {
		throw new NotImplementedException();
	}

	public ModelData GetVictoryModel(Level level) {
		throw new NotImplementedException();
	}

	public string GetVictoryStandby() {
		throw new NotImplementedException();
	}
}
