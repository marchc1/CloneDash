using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Compatibility.MuseDash;

public class CharacterLocalizationData {
	[JsonProperty("characterName")] public string CharacterName;
	[JsonProperty("cosName")] public string CosName;
	[JsonProperty("cosNames")] public string[] CosNames;
	[JsonProperty("description")] public string Description;
	[JsonProperty("skill")] public string Skill;
	[JsonProperty("cv")] public string CV;
	[JsonProperty("cvs")] public string[] CVs;
	[JsonProperty("expressions")] public string[][] Expressions;
}
public class CharacterConfigData
{
	[JsonProperty("characterName")] public string CharacterName { get; set; }
	[JsonProperty("cosName")] public string CosName { get; set; }
	[JsonProperty("description")] public string Description { get; set; }
	[JsonProperty("defaultHP")] public string DefaultHP { get; set; }
	[JsonProperty("skillName")] public string SkillName { get; set; }
	[JsonProperty("skill")] public string Skill { get; set; }
	[JsonProperty("chipName")] public string ChipName { get; set; }
	[JsonProperty("chipDescription")] public string ChipDescription { get; set; }
	[JsonProperty("cv")] public string Cv { get; set; }
	[JsonProperty("chipAmount")] public int ChipAmount { get; set; }
	[JsonProperty("chipImage")] public string ChipImage { get; set; }
	[JsonProperty("mainShow")] public string MainShow { get; set; }
	[JsonProperty("battleShow")] public string BattleShow { get; set; }
	[JsonProperty("feverShow")] public string FeverShow { get; set; }
	[JsonProperty("atkFxGreat")] public string AtkFxGreat { get; set; }
	[JsonProperty("atkFxPerfect")] public string AtkFxPerfect { get; set; }
	[JsonProperty("atkFxCrit")] public string AtkFxCrit { get; set; }
	[JsonProperty("jumpFxDust")] public object JumpFxDust { get; set; }
	[JsonProperty("victoryShow")] public string VictoryShow { get; set; }
	[JsonProperty("failShow")] public string FailShow { get; set; }
	[JsonProperty("bgm")] public string BGM { get; set; }
	[JsonProperty("unlockType")] public int UnlockType { get; set; }
	[JsonProperty("unlockCount")] public int UnlockCount { get; set; }
	[JsonProperty("rarity")] public string Rarity { get; set; }
	[JsonProperty("free")] public bool Free { get; set; }
	[JsonProperty("hide")] public bool Hide { get; set; }
	[JsonProperty("egg")] public bool Egg { get; set; }
	[JsonProperty("exchange")] public bool Exchange { get; set; }
	[JsonProperty("special")] public bool Special { get; set; }
	[JsonProperty("characterType")] public string CharacterType { get; set; }
	[JsonProperty("releasedVersion")] public object ReleasedVersion { get; set; }
	[JsonProperty("order")] public int Order { get; set; }
	[JsonProperty("expressions")] public List<CharacterExpression> Expressions { get; set; }
	[JsonProperty("listIndex")] public int ListIndex { get; set; }
}

public class CharacterExpression
{
	[JsonProperty("animName")] public string AnimName { get; set; }
	[JsonProperty("audioNames")] public List<string> AudioNames { get; set; }
	[JsonProperty("talkInfosList")] public object TalkInfosList { get; set; }
	[JsonProperty("weight")] public double Weight { get; set; }
}