using Newtonsoft.Json;

namespace CloneDash.Modding.Descriptors
{
	public class CharacterDescriptor : CloneDashDescriptor
	{
		public CharacterDescriptor() : base(CloneDashDescriptorType.Character) { }

		[JsonIgnore] private static readonly string[] Attacks = ["Hit1", "Hit2", "Hit3"];

		public string Animation_WalkCycle = "Walk";
		public string[] Animation_AirAttacks_Failed = ["Jump"];
		public string[] Animation_GroundAttacks_Failed = ["Punch"];

		public string[]? Animation_Attacks = null;
		public string[]? Animation_AirAttacks = null;
		public string[]? Animation_GroundAttacks = Attacks;

		public string[] GetAirAttacks() =>
			Animation_AirAttacks == null ? Animation_Attacks ?? throw new Exception("No air attacks!") : Animation_AirAttacks;
		public string[] GetGroundAttacks() =>
			Animation_GroundAttacks == null ? Animation_Attacks ?? throw new Exception("No ground attacks!") : Animation_GroundAttacks;

		public string Animation_Holding = "Holding";

		public static CharacterDescriptor ParseFile(string filepath) => ParseFile<CharacterDescriptor>(filepath);
	}
}
