using AssetStudio;
using CloneDash.Compatibility.Unity;

namespace CloneDash.Compatibility.MuseDash;

public static class MuseDash1VoiceSetting
{
	public const string FAIL = "m_FailVoices";
	public const string VICTORY = "m_VictoryVoices";

	static Dictionary<string, Dictionary<int, List<string>>>? voices;

	public static string? GetRandomVoice(string list, int characterIndex) {
		voices ??= Load();
		if (!voices.TryGetValue(list, out var entries) || !entries.TryGetValue(characterIndex, out var names) || names.Count == 0)
			return null;
		return names[Random.Shared.Next(names.Count)];
	}

	static Dictionary<string, Dictionary<int, List<string>>> Load() {
		Dictionary<string, Dictionary<int, List<string>>> result = [];
		var setting = MuseDash1Compatibility.StreamingAssets.FindAssetByName<MonoBehaviour>("VoiceSetting");
		if (setting == null || !UnityOdinSerialization.TryGetSerializationData(setting.ToType(), out var bytes, out _))
			return result;

		Dictionary<int, List<string>>? entries = null;
		List<string>? names = null;
		int index = -1;
		foreach (var token in UnityOdinSerialization.Tokenize(bytes)) {
			if (token.Type == UnityOdinSerialization.NamedStartOfReferenceNode && token.Name is string list && list.EndsWith("Voices")) {
				result[list] = entries = [];
				names = null;
				continue;
			}
			if (entries == null)
				continue;

			if (token.Name == "index" && token.Value is int entryIndex) {
				index = entryIndex;
				names = entries[index] = [];
			}
			else if (token.Name == "catNames")
				names = null;
			else if (token.Type == UnityOdinSerialization.UnnamedString && token.Value is string name && names != null)
				names.Add(name);
		}
		return result;
	}
}
