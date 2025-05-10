using CloneDash.Game;
using CloneDash.Modding.Descriptors;
using CloneDash.Modding.Settings;
using Nucleus.Audio;
using Nucleus.Engine;
using Nucleus.Models.Runtime;

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
		throw new NotImplementedException();
	}

	public MusicTrack? GetMainShowMusic(Level level) {
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
