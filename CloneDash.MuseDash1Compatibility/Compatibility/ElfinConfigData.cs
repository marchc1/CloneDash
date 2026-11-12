using Newtonsoft.Json;
using System.Globalization;

namespace CloneDash.Compatibility.MuseDash;

public class ElfinLocalizationData
{
	[JsonProperty("name")] public string Name;
	[JsonProperty("description")] public string Description;
	[JsonProperty("skill")] public string Skill;
	[JsonProperty("chipName")] public string ChipName;
	[JsonProperty("chipDescription")] public string ChipDescription;
}

public class ElfinConfigData
{
	public readonly Dictionary<CultureInfo, ElfinLocalizationData> Localization = [];

	public int Index { get; internal set; }

	[JsonProperty("name")] public string Name { get; set; }
	[JsonProperty("description")] public string Description { get; set; }
	[JsonProperty("skill")] public string Skill { get; set; }
	[JsonProperty("chipAmount")] public int ChipAmount { get; set; }
	[JsonProperty("prefab")] public string Prefab { get; set; }
	[JsonProperty("mainShow")] public string MainShow { get; set; }
	[JsonProperty("chipName")] public string ChipName { get; set; }
	[JsonProperty("chipDescription")] public string ChipDescription { get; set; }
	[JsonProperty("chipImage")] public string ChipImage { get; set; }
	[JsonProperty("rarity")] public string Rarity { get; set; }
	[JsonProperty("free")] public bool Free { get; set; }
	[JsonProperty("order")] public int Order { get; set; }
	[JsonProperty("hide")] public bool Hide { get; set; }
	[JsonProperty("egg")] public bool Egg { get; set; }
	[JsonProperty("achShow")] public string AchShow { get; set; }
	[JsonProperty("thumb")] public string Thumb { get; set; }

	internal void PrepareLocalization() {
		Name = $"#{Prefab}_{nameof(Name)}";
		Description = $"#{Prefab}_{nameof(Description)}";
		Skill = $"#{Prefab}_{nameof(Skill)}";
		ChipName = $"#{Prefab}_{nameof(ChipName)}";
		ChipDescription = $"#{Prefab}_{nameof(ChipDescription)}";
	}

	internal void AddLocalization(CultureInfo lang, ElfinLocalizationData elfinLocalizationData) {
		Localization[lang] = elfinLocalizationData;

		localize.AddString($"#{Prefab}_{nameof(Name)}", elfinLocalizationData.Name, lang);
		localize.AddString($"#{Prefab}_{nameof(Description)}", elfinLocalizationData.Description, lang);
		localize.AddString($"#{Prefab}_{nameof(Skill)}", elfinLocalizationData.Skill, lang);
		localize.AddString($"#{Prefab}_{nameof(ChipName)}", elfinLocalizationData.ChipName, lang);
		localize.AddString($"#{Prefab}_{nameof(ChipDescription)}", elfinLocalizationData.ChipDescription, lang);
	}
}
