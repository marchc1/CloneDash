using CloneDash.Compatibility.MuseDash;
using Newtonsoft.Json;
using Nucleus.Audio;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.Models.Runtime;

namespace CloneDash.Modding.Descriptors
{
	/// <summary>
	/// Class used for in-game animations from <see cref="CharacterDescriptor_MainShowTouch.GetRandomTouchResponse()"/>
	/// </summary>
	public class CharacterMainShowTouchResponse(CharacterDescriptor_MainShowTouch touchdata, string response) {
		public Descriptor_MultiAnimationClass MainResponse => touchdata.MainResponse;

		public string Start => string.Format(touchdata.StartResponse, response);
		public string Standby => string.Format(touchdata.StandbyResponse, response);
		public string End => string.Format(touchdata.EndResponse, response);
	}
	public class CharacterDescriptor_MainShowTouch {
		[JsonProperty("response_main")] public Descriptor_MultiAnimationClass? MainResponse;
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
		[JsonProperty("mdimage")] public string? UseMDImage;

		[JsonProperty("music")] public string? Music;
		[JsonProperty("mdmusic")] public string? MDMusic;

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
		[JsonProperty("mdimage")] public string? UseMDImage;

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
		[JsonProperty("mdimage")] public string? UseMDImage;
		[JsonProperty("standby")] public string Standby;
	}
	public class CharacterDescriptor_Fail
	{
		[JsonProperty("model")] public string Model;
		[JsonProperty("mdimage")] public string? UseMDImage;
	}
	public class CharacterDescriptor : CloneDashDescriptor
	{
		public CharacterDescriptor() : base(CloneDashDescriptorType.Character, "chars", "character", "character", "2025-05-06-01") { }

		[JsonProperty("name")] public string Name;
		[JsonProperty("author")] public string Author;
		[JsonProperty("perk")] public string Perk;

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

		public string GetVictoryModel() => Victory.Model;

		public MusicTrack? GetMainShowMusic(Level level) {
			if(MainShow.Music != null) 
				return level.Sounds.LoadMusicFromFile("character", MainShow.Music);

			if (MainShow.MDMusic != null)
				return MuseDashCompatibility.GenerateMusicTrack(level, MainShow.MDMusic);

			return null;
		}


		public ModelData GetPlayModelData(Level level) {
			var play = Play;
			var cached = level.Models.IsCached("character", play.Model);
			if(MuseDashModelConverter.ShouldLoadMDModel(play.Model, out string outPath)) {
				ModelData md_data = new ModelData();
				MuseDashModelConverter.ConvertMuseDashModelData(md_data, outPath, MuseDashCompatibility.PopulateModelDataTextures(md_data, outPath));
				return md_data;
			}
			var data = level.Models.LoadModelFromFile("character", play.Model);
			if (play.UseMDImage != null && !cached)
				MuseDashCompatibility.PopulateModelDataTextures(data, play.UseMDImage);

			return data;
		}

		public ModelData GetMainShowModelData(Level level) {
			var mainshow = MainShow;
			var cached = level.Models.IsCached("character", mainshow.Model);
			if (MuseDashModelConverter.ShouldLoadMDModel(mainshow.Model, out string outPath)) {
				ModelData md_data = new ModelData();
				MuseDashModelConverter.ConvertMuseDashModelData(md_data, outPath, MuseDashCompatibility.PopulateModelDataTextures(md_data, outPath));
				return md_data;
			}
			var data = level.Models.LoadModelFromFile("character", mainshow.Model);
			if (mainshow.UseMDImage != null && !cached)
				MuseDashCompatibility.PopulateModelDataTextures(data, mainshow.UseMDImage);

			return data;
		}
		public ModelData GetVictoryModelData(Level level) {
			var victory = Victory;
			var cached = level.Models.IsCached("character", victory.Model);
			if (MuseDashModelConverter.ShouldLoadMDModel(victory.Model, out string outPath)) {
				ModelData md_data = new ModelData();
				MuseDashModelConverter.ConvertMuseDashModelData(md_data, outPath, MuseDashCompatibility.PopulateModelDataTextures(md_data, outPath));
				return md_data;
			}
			var data = level.Models.LoadModelFromFile("character", Play.Model);
			if (victory.UseMDImage != null && !cached)
				MuseDashCompatibility.PopulateModelDataTextures(data, victory.UseMDImage);

			return data;
		}
	}
}