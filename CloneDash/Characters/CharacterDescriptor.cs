using CloneDash.Characters;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Newtonsoft.Json;
using Nucleus.Audio;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Models.Runtime;

namespace CloneDash.Modding.Descriptors;

public class CharacterDescriptor_MainShowExpressionText {
	[JsonProperty("text")] public string Text;
	[JsonProperty("voice")] public string Voice;
}
public class CharacterDescriptor_MainShowExpression : ICharacterExpression
{
	[JsonProperty("standby")] public string? Standby;

	[JsonProperty("face_start")] public string Start;
	[JsonProperty("face_idle")] public string Idle;
	[JsonProperty("face_end")] public string End;

	[JsonProperty("responses")] public CharacterDescriptor_MainShowExpressionText[] Responses;

	string ICharacterExpression.GetStartAnimationName() => Start;
	string ICharacterExpression.GetIdleAnimationName() => Idle;
	string ICharacterExpression.GetEndAnimationName() => End;
	void ICharacterExpression.GetSpeech(Level level, out string text, out Sound sound) {
		var item = Responses.Random();
		text = item.Text;
		sound = level.Sounds.LoadSoundFromFile("character", item.Voice);
	}
}
public class CharacterDescriptor_MainShow
{
	[JsonProperty("model")] public string Model;
	[JsonProperty("music")] public string? Music;
	[JsonProperty("standby")] public string Standby;
	[JsonProperty("clicked")] public string? ClickAnimation;
	[JsonProperty("expressions")] public CharacterDescriptor_MainShowExpression[] Expressions;
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
	[JsonProperty("standby")] public string Standby;
}
public class CharacterDescriptor_Fail
{
	[JsonProperty("model")] public string Model;
}
public class CharacterDescriptor : CloneDashDescriptor, ICharacterDescriptor
{
	public CharacterDescriptor() : base(CloneDashDescriptorType.Character, "chars", "character", "character", "2025-05-06-01") { }

	[JsonProperty("name")] public string Name { get; set; }
	[JsonProperty("description")] public string? Description { get; set; }
	[JsonProperty("author")] public string Author { get; set; }
	[JsonProperty("perk")] public string Perk { get; set; }

	/// <summary>
	/// Maximum player health
	/// </summary>
	[JsonProperty("default_hp")] public double DefaultHP { get; set; }
	/// <summary>
	/// Specifies a string filepath (local to the directory of the descriptor) for a Lua logic controller.
	/// Not implemented yet, requires my plans for a scripting API
	/// </summary>
	[JsonProperty("logic_controller")] public string? LogicController { get; set; }

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

	string ICharacterDescriptor.GetName() => Name;
	string? ICharacterDescriptor.GetDescription() => Description;
	string ICharacterDescriptor.GetAuthor() => Author;
	string ICharacterDescriptor.GetPerk() => Perk;

	ModelData ICharacterDescriptor.GetPlayModel(Level level) => level.Models.LoadModelFromFile("character", Play.Model);
	ModelData ICharacterDescriptor.GetMainShowModel(Level level) => level.Models.LoadModelFromFile("character", MainShow.Model);
	ModelData ICharacterDescriptor.GetVictoryModel(Level level) => level.Models.LoadModelFromFile("character", Victory.Model);
	ModelData ICharacterDescriptor.GetFailModel(Level level) => level.Models.LoadModelFromFile("character", Fail.Model);
	MusicTrack? ICharacterDescriptor.GetMainShowMusic(Level level) => MainShow.Music == null ? null : level.Sounds.LoadMusicFromFile("character", MainShow.Music);

	string ICharacterDescriptor.GetMainShowStandby() => MainShow.Standby;
	string ICharacterDescriptor.GetVictoryStandby() => Victory.Standby;

	ICharacterExpression? ICharacterDescriptor.GetMainShowExpression() {
		var exp = MainShow.Expressions?.Random();
		return exp;
	}

	double ICharacterDescriptor.GetDefaultHP() => DefaultHP;
	string? ICharacterDescriptor.GetLogicControllerData() => LogicController == null ? null : Filesystem.ReadAllText("character", LogicController);

	string ICharacterDescriptor.GetPlayAnimation(CharacterAnimationType type) {
		var playData = Play;

		switch (type) {
			case CharacterAnimationType.Run: return playData.RunAnimation.GetAnimation();
			case CharacterAnimationType.In: return playData.InAnimation ?? playData.RunAnimation.GetAnimation();
			case CharacterAnimationType.Die: return playData.DieAnimation.GetAnimation();

			case CharacterAnimationType.AirGreat: return playData.AirAnimations.Great.GetAnimation();
			case CharacterAnimationType.AirPerfect: return playData.AirAnimations.Perfect.GetAnimation();
			case CharacterAnimationType.AirHurt: return playData.AirAnimations.Hurt.GetAnimation();

			case CharacterAnimationType.Double: return playData.DoubleAnimation.GetAnimation();

			case CharacterAnimationType.Jump: return playData.JumpAnimations.Jump.GetAnimation();
			case CharacterAnimationType.JumpHurt: return playData.JumpAnimations.Hurt.GetAnimation();

			case CharacterAnimationType.RoadGreat: return playData.RoadAnimations.Great.GetAnimation();
			case CharacterAnimationType.RoadPerfect: return playData.RoadAnimations.Perfect.GetAnimation();
			case CharacterAnimationType.RoadMiss: return playData.RoadAnimations.Miss.GetAnimation();
			case CharacterAnimationType.RoadHurt: return playData.RoadAnimations.Hurt.GetAnimation();

			case CharacterAnimationType.Press: return playData.PressAnimations.Press.GetAnimation();
			case CharacterAnimationType.AirPressEnd: return playData.PressAnimations.AirPressEnd.GetAnimation();

			default: throw new Exception("Can't do anything here");
		}

		throw new Exception("Can't do anything here");
	}

	string? ICharacterDescriptor.GetMainShowInitialExpression() => MainShow.ClickAnimation;
}