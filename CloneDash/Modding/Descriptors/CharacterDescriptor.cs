using Newtonsoft.Json;
using Nucleus.Core;

namespace CloneDash.Modding.Descriptors
{
	/// <summary>
	/// Class used for in-game animations from <see cref="CharacterDescriptor_MainShowTouch.GetRandomTouchResponse()"/>
	/// </summary>
	public class CharacterMainShowTouchResponse(CharacterDescriptor_MainShowTouch touchdata, string response) {
		public Descriptor_MultiAnimationClass MainResponse => touchdata.MainResponse;

		public string Start => string.Format(touchdata.StartResponse, response);
		public string Standby => string.Format(touchdata.StartResponse, response);
		public string End => string.Format(touchdata.StartResponse, response);
	}
	public class CharacterDescriptor_MainShowTouch {
		[JsonProperty("response_main")] public Descriptor_MultiAnimationClass MainResponse;
		[JsonProperty("response_start")] public string StartResponse;
		[JsonProperty("response_standby")] public string StandbyResponse;
		[JsonProperty("response_end")] public string EndResponse;
		[JsonProperty("responses")] public string[] Responses;

		public CharacterMainShowTouchResponse GetRandomTouchResponse() {
			var str = Responses[Random.Shared.Next(0, Responses.Length)];
			return new(this, str);
		}
	}
	public class CharacterDescriptor_MainShow
	{
		[JsonProperty("model")] public string Model;
		[JsonProperty("music")] public string Music;
		[JsonProperty("standby")] public string StandbyAnimation;
		[JsonProperty("touch")] public CharacterDescriptor_MainShowTouch Touch;
	}
	public class CharacterDescriptor_Play
	{
		public class CharacterDescriptor_PlayAir
		{
			[JsonProperty("great")] public Descriptor_MultiAnimationClass Great;
			[JsonProperty("perfect")] public Descriptor_MultiAnimationClass Perfect;
			[JsonProperty("hurt")] public Descriptor_MultiAnimationClass Hurt;
			[JsonProperty("miss")] public Descriptor_MultiAnimationClass? Miss;
		}
		public class CharacterDescriptor_PlayRoad
		{
			[JsonProperty("great")] public Descriptor_MultiAnimationClass Great;
			[JsonProperty("perfect")] public Descriptor_MultiAnimationClass Perfect;
			[JsonProperty("hurt")] public Descriptor_MultiAnimationClass Hurt;
			[JsonProperty("miss")] public Descriptor_MultiAnimationClass Miss;
		}
		public class CharacterDescriptor_PlayTransitions
		{
			[JsonProperty("air_to_ground")] public Descriptor_MultiAnimationClass AirToGround;
		}
		public class CharacterDescriptor_PlayJump
		{
			[JsonProperty("jump")] public Descriptor_MultiAnimationClass Jump;
			[JsonProperty("hurt")] public Descriptor_MultiAnimationClass Hurt;
		}
		public class CharacterDescriptor_PlayPress
		{
			[JsonProperty("press")] public Descriptor_MultiAnimationClass Press;

			[JsonProperty("air_press_end")] public Descriptor_MultiAnimationClass AirPressEnd;
			[JsonProperty("air_press_hurt")] public Descriptor_MultiAnimationClass AirPressHurt;

			[JsonProperty("down_press_hit")] public Descriptor_MultiAnimationClass DownPressHit;
			[JsonProperty("up_press_hit")] public Descriptor_MultiAnimationClass UpPressHit;
		}

		[JsonProperty("model")] public string Model;

		[JsonProperty("run")] public Descriptor_MultiAnimationClass RunAnimation;
		[JsonProperty("in")] public string? InAnimation;
		[JsonProperty("die")] public Descriptor_MultiAnimationClass DieAnimation;
		[JsonProperty("standby")] public Descriptor_MultiAnimationClass StandbyAnimation;

		[JsonProperty("air")] public CharacterDescriptor_PlayAir AirAnimations;
		[JsonProperty("road")] public CharacterDescriptor_PlayRoad RoadAnimations;
		[JsonProperty("double")] public Descriptor_MultiAnimationClass DoubleAnimation;
		[JsonProperty("transitions")] public CharacterDescriptor_PlayTransitions TransitionAnimations;
		[JsonProperty("jump")] public CharacterDescriptor_PlayJump JumpAnimations;
		[JsonProperty("press")] public CharacterDescriptor_PlayPress PressAnimations;

	}
	public class CharacterDescriptor_Victory
	{
		[JsonProperty("model")] public string Model;
	}
	public class CharacterDescriptor_Fail
	{
		[JsonProperty("model")] public string Model;
	}
	public class CharacterDescriptor : CloneDashDescriptor
	{
		public CharacterDescriptor() : base(CloneDashDescriptorType.Character, "4") { }

		[JsonProperty("name")] public string Name;
		[JsonProperty("author")] public string Author;
		[JsonProperty("perk")] public string Perk;
		[JsonProperty("version")] public string Version;


		/// <summary>
		/// Maximum player health
		/// </summary>
		[JsonProperty("max_hp")] public double? MaxHP;
		/// <summary>
		/// Fever threshold
		/// </summary>
		[JsonProperty("fever_threshold")] public double? FeverThreshold;
		/// <summary>
		/// How long a fever lasts, in seconds
		/// </summary>
		[JsonProperty("fever_time")] public double? FeverTime;
		/// <summary>
		/// Is the character killable. If false, deaths will not be registered beyond
		/// the final score being inapplicable
		/// </summary>
		[JsonProperty("killable")] public bool Killable = true;
		/// <summary>
		/// How fast HP is lost over time (hp/per sec)
		/// </summary>
		[JsonProperty("lose_hp_rate")] public double LoseHPRate;
		/// <summary>
		/// Does this character enter autoplay mode.
		/// </summary>
		[JsonProperty("automatic")] public bool Automatic;
		/// <summary>
		/// Score multiplier.
		/// </summary>
		[JsonProperty("score_multiplier")] public double ScoreMultiplier = 1.0d;
		/// <summary>
		/// Specifies a string filepath (local to the directory of the descriptor) for a Lua logic controller.
		/// Not implemented yet, requires my plans for a scripting API
		/// </summary>
		[JsonProperty("logic_controller")] public string? LogicController;

		/// <summary>
		/// Main menu model
		/// </summary>
		[JsonProperty("mainshow")] public CharacterDescriptor_MainShow MainShow = new();
		/// <summary>
		/// In-game model
		/// </summary>
		[JsonProperty("play")] public CharacterDescriptor_Play Play = new();
		/// <summary>
		/// Victory model
		/// </summary>
		[JsonProperty("victory")] public CharacterDescriptor_Victory Victory = new();
		/// <summary>
		/// Fail model
		/// </summary>
		[JsonProperty("fail")] public CharacterDescriptor_Fail Fail = new();

		public static CharacterDescriptor? ParseCharacter(string filename) => Filesystem.ReadAllText("chars", filename, out var text) ? ParseFile<CharacterDescriptor>(text, filename) : null;

		public string GetPlayModel() => Play.Model;
		public string GetMainShowModel() => MainShow.Model;
		public string GetMainShowMusic() => MainShow.Music;

		internal void MountToFilesystem() {
			Filesystem.RemoveSearchPath("character");
			var searchPath = Filesystem.FindSearchPath("chars", $"{Filename}/character.cdd");
			switch (searchPath) {
				case DiskSearchPath diskPath:
					Filesystem.AddTemporarySearchPath("character", DiskSearchPath.Combine(searchPath, Filename));
					break;
			}
		}
	}
}