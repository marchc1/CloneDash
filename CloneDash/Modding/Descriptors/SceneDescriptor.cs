// This entire file is a nightmare of inheritance.

using CloneDash.Game;
using CloneDash.Settings;
using Newtonsoft.Json;

using Nucleus.Audio;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
namespace CloneDash.Modding.Descriptors;

public class SceneDescriptor : CloneDashDescriptor
{
#nullable disable
	public SceneDescriptor() : base(CloneDashDescriptorType.Scene, "scenes", "scene", "scene", "2025-05-06-01") { }
#nullable enable
	public abstract class SceneDescriptor_ContainsOneModelData
	{
#nullable disable
		[JsonProperty("model")] public string Model;

		[JsonIgnore] public ModelData ModelData;
#nullable enable
		public void LoadModelData(Level level) {
			ModelData = level.Models.LoadModelFromFile("scene", Model);
		}
	}
	public abstract class SceneDescriptor_ContainsAirGroundModelData
	{
#nullable disable
		[JsonProperty("airmodel")] public string AirModel;
		[JsonProperty("groundmodel")] public string GroundModel;

		[JsonIgnore] public ModelData AirModelData;
		[JsonIgnore] public ModelData GroundModelData;
#nullable enable
		public virtual void LoadModelData(Level level) {
			AirModelData = level.Models.LoadModelFromFile("scene", AirModel);
			GroundModelData = level.Models.LoadModelFromFile("scene", GroundModel);
		}

		public virtual ModelData GetModelFromPathway(PathwaySide pathway)
			=> pathway == PathwaySide.Top ? AirModelData :
			pathway == PathwaySide.Bottom ? GroundModelData :
			throw new InvalidOperationException("No way to give this entity a model when it doesnt have a pathway.");
	}

	public class SceneDescriptor_Announcer
	{
#nullable disable
		[JsonProperty("begin")] public string Begin;
		[JsonProperty("fever")] public string Fever;
		[JsonProperty("unpause")] public string Unpause;
		[JsonProperty("fullcombo")] public string FullCombo;

		[JsonIgnore] public Sound BeginSound;
		[JsonIgnore] public Sound FeverSound;
		[JsonIgnore] public Sound UnpauseSound;
		[JsonIgnore] public Sound FullComboSound;
#nullable enable
	}
	public class SceneDescriptor_Boss : SceneDescriptor_ContainsOneModelData
	{
#nullable disable
		[JsonProperty("in")] public string In;
		[JsonProperty("out")] public string Out;
		[JsonProperty("hurt")] public string Hurt;

		[JsonProperty("standby")] public SceneDescriptor_BossStandby Standby;
		[JsonProperty("attacks")] public SceneDescriptor_BossAttacks Attacks;
		[JsonProperty("transitions")] public SceneDescriptor_BossTransitions Transitions;
		[JsonProperty("close")] public SceneDescriptor_BossClose Close;
		[JsonProperty("multi")] public SceneDescriptor_BossMulti Multi;
#nullable enable
	}

#nullable disable
	public class SceneDescriptor_OneSustain
	{
		[JsonProperty("start")] public string Start;
		[JsonProperty("end")] public string End;
		[JsonProperty("body")] public string Body;
		[JsonProperty("up")] public string Up;
		[JsonProperty("down")] public string Down;

		[JsonIgnore] public Texture StartTexture;
		[JsonIgnore] public Texture EndTexture;
		[JsonIgnore] public Texture BodyTexture;
		[JsonIgnore] public Texture UpTexture;
		[JsonIgnore] public Texture DownTexture;

		public void LoadData(Level level) {
			StartTexture = level.Textures.LoadTextureFromFile("scene", Start);
			EndTexture = level.Textures.LoadTextureFromFile("scene", End);
			BodyTexture = level.Textures.LoadTextureFromFile("scene", Body);
			UpTexture = level.Textures.LoadTextureFromFile("scene", Up);
			DownTexture = level.Textures.LoadTextureFromFile("scene", Down);

			BodyTexture.SetWrap(Raylib_cs.TextureWrap.TEXTURE_WRAP_REPEAT);
			UpTexture.SetWrap(Raylib_cs.TextureWrap.TEXTURE_WRAP_REPEAT);
			DownTexture.SetWrap(Raylib_cs.TextureWrap.TEXTURE_WRAP_REPEAT);
		}
	}
	public class SceneDescriptor_Sustains
	{
		[JsonProperty("degrees")] public float RotationDegsPerSecond = 120;
		[JsonProperty("air")] public SceneDescriptor_OneSustain Air;
		[JsonProperty("ground")] public SceneDescriptor_OneSustain Ground;
		public void LoadData(Level level) {
			Air.LoadData(level);
			Ground.LoadData(level);
		}

		public Texture GetStartTexture(PathwaySide pathway) => pathway == PathwaySide.Top ? Air.StartTexture : Ground.StartTexture;
		public Texture GetEndTexture(PathwaySide pathway) => pathway == PathwaySide.Top ? Air.EndTexture : Ground.EndTexture;
		public Texture GetBodyTexture(PathwaySide pathway) => pathway == PathwaySide.Top ? Air.BodyTexture : Ground.BodyTexture;
		public Texture GetUpTexture(PathwaySide pathway) => pathway == PathwaySide.Top ? Air.UpTexture : Ground.UpTexture;
		public Texture GetDownTexture(PathwaySide pathway) => pathway == PathwaySide.Top ? Air.DownTexture : Ground.DownTexture;
	}
#nullable enable

	public interface IContains3Speeds
	{
		public int LowSpeed { get; }
		public int MediumSpeed { get; }
		public int HighSpeed { get; }
	}

	public interface IContainsGreatPerfect
	{
		public Nucleus.Models.Runtime.Animation FindGreatAnimation(ModelInstance model);
		public Nucleus.Models.Runtime.Animation FindPerfectAnimation(ModelInstance model);
	}

	public interface IContainsGreatPerfectAndHPMount : IContainsGreatPerfect
	{
		public BoneInstance GetHPMount(ModelInstance model);
	}


	public class SceneDescriptor_ContainsOneModelData_With3Speeds : SceneDescriptor_ContainsOneModelData, IContains3Speeds
	{
#nullable disable
		[JsonProperty("speeds")] public int[] Speeds;
		public int LowSpeed => Speeds[2];
		public int MediumSpeed => Speeds[1];
		public int HighSpeed => Speeds[0];
#nullable enable
	}

	public class SceneDescriptor_Gears
	{
#nullable disable
		public class __Gear : SceneDescriptor_ContainsOneModelData_With3Speeds
		{
			[JsonProperty("format")] public string Format;
			[JsonProperty("destroy")] public string Destroy;
			public string GetAnimationString(int speed, out double showtime) {
				var speedIndex = speed switch {
					1 => 2,
					2 => 1,
					3 => 0,
					_ => throw new Exception("Invalid speed")
				};

				var frameSpeed = Speeds[speedIndex];
				showtime = frameSpeed / Model4System.REFERENCE_FPS;
				return string.Format(Format, frameSpeed);
			}
		}

		[JsonProperty("air")] public __Gear Air;
		[JsonProperty("ground")] public __Gear Ground;

		public void LoadModelData(Level level) {
			Air.LoadModelData(level);
			Ground.LoadModelData(level);
		}
		public virtual ModelData GetModelFromPathway(PathwaySide pathway) => pathway switch {
			PathwaySide.Top => Air.ModelData,
			PathwaySide.Bottom => Ground.ModelData,
			_ => throw new Exception("Invalid pathway.")
		};

		public string GetAnimationString(PathwaySide pathway, int speed, out double showtime) => pathway switch {
			PathwaySide.Top => Ground.GetAnimationString(speed, out showtime),
			PathwaySide.Bottom => Ground.GetAnimationString(speed, out showtime),
			_ => throw new Exception("Invalid pathway.")
		};

#nullable enable
	}

	public class SceneDescriptor_BossGears
	{
#nullable disable
		public class __BossGear : SceneDescriptor_ContainsOneModelData
		{
			[JsonProperty("format_1")] public string Format1;
			[JsonProperty("format_2")] public string Format2;
			[JsonProperty("speeds_1")] public int[] Speeds1;
			[JsonProperty("speeds_2")] public int[] Speeds2;
			[JsonProperty("destroy")] public string Destroy;

			public string GetAnimationString(int speed, out double showtime) {
				var speedIndex = speed switch {
					1 => 2,
					2 => 1,
					3 => 0,
					_ => throw new Exception("Invalid speed")
				};

				var format = speed == 1 ? Format1 : Format2;
				var speeds = speed == 1 ? Speeds1 : Speeds2;

				var frameSpeed = speeds[speedIndex];
				showtime = frameSpeed / Model4System.REFERENCE_FPS;
				return string.Format(format, frameSpeed);
			}
		}
		[JsonProperty("air")] public __BossGear Air;
		[JsonProperty("ground")] public __BossGear Ground;

		public void LoadModelData(Level level) {
			Air.LoadModelData(level);
			Ground.LoadModelData(level);
		}

		public virtual ModelData GetModelFromPathway(PathwaySide pathway) => pathway switch {
			PathwaySide.Top => Air.ModelData,
			PathwaySide.Bottom => Ground.ModelData,
			_ => throw new Exception("Invalid pathway.")
		};
		public string GetAnimationString(PathwaySide pathway, int speed, out double showtime) => pathway switch {
			PathwaySide.Top => Ground.GetAnimationString(speed, out showtime),
			PathwaySide.Bottom => Ground.GetAnimationString(speed, out showtime),
			_ => throw new Exception("Invalid pathway.")
		};

#nullable enable
	}

	public class SceneDescriptor_MasherEnemy : SceneDescriptor_ContainsOneModelData
	{
#nullable disable
		public class __Animations
		{

			[JsonProperty("format")] public string Format;
			[JsonProperty("speeds")] public int[] Speeds;
		}
		public class __InAnimations
		{
			[JsonProperty("down")] public __Animations Down;
			[JsonProperty("normal")] public __Animations Normal;
		}
		public class __MissAnimations
		{
			[JsonProperty("format")] public string Format;
			[JsonProperty("normal")] public int[] NormalSpeeds;
		}
		public class __CompleteAnimations : IContainsGreatPerfect
		{
			public Nucleus.Models.Runtime.Animation FindGreatAnimation(ModelInstance model) => model.Data.FindAnimation(Great);
			public Nucleus.Models.Runtime.Animation FindPerfectAnimation(ModelInstance model) => model.Data.FindAnimation(Perfect);
			[JsonProperty("great")] public string Great;
			[JsonProperty("perfect")] public string Perfect;
		}

		[JsonProperty("hurt")] public Descriptor_MultiAnimationClass Hurt;
		[JsonProperty("in")] public __InAnimations InAnimations;
		[JsonProperty("miss")] public __MissAnimations MissAnimations;
		[JsonProperty("complete")] public __CompleteAnimations CompleteAnimations;
#nullable enable
	}
	public class SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation : SceneDescriptor_ContainsAirGroundModelData, IContains3Speeds, IContainsGreatPerfect
	{
#nullable disable
		public Nucleus.Models.Runtime.Animation FindGreatAnimation(ModelInstance model) => model.Data.FindAnimation(Great);
		public Nucleus.Models.Runtime.Animation FindPerfectAnimation(ModelInstance model) => model.Data.FindAnimation(Perfect);

		[JsonProperty("speeds")] public int[] Speeds;
		[JsonProperty("animation")] public string Animation;
		[JsonProperty("great")] public string Great;
		[JsonProperty("perfect")] public string Perfect;
		[JsonIgnore] public int LowSpeed => Speeds[2];
		[JsonIgnore] public int MediumSpeed => Speeds[1];
		[JsonIgnore] public int HighSpeed => Speeds[0];

		public string GetAnimationString(int speed, out double showtime) {
			var speedIndex = speed switch {
				1 => 2,
				2 => 1,
				3 => 0,
				_ => throw new Exception("Invalid speed")
			};

			var frameSpeed = Speeds[speedIndex];
			showtime = frameSpeed / Model4System.REFERENCE_FPS;
			return string.Format(Animation, frameSpeed);
		}
#nullable enable
	}
	public class SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation, IContains3Speeds, IContainsGreatPerfectAndHPMount
	{
#nullable disable
		public BoneInstance GetHPMount(ModelInstance model) => model.FindBone(HPMount) ?? throw new KeyNotFoundException($"Missing mount bone '{HPMount}' for {GetType().Name}. Either the scene descriptor failed to add a 'hp' bone in the model, or failed to define a custom name override.");

		[JsonProperty("hp_mount")] public string HPMount = "hp";
#nullable enable
	}

	public class SceneDescriptor_DoubleEnemy : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation;
	public class SceneDescriptor_BossEnemy1 : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount;
	public class SceneDescriptor_BossEnemy2 : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount;
	public class SceneDescriptor_BossEnemy3 : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount;

	public class SceneDescriptor_FormatSpeeds : IContains3Speeds
	{
#nullable disable
		[JsonProperty("xoffset")] public float XOffset = 0;
		[JsonProperty("format")] public string Format;
		[JsonProperty("speeds")] public int[] Speeds;
		public int LowSpeed => Speeds[2];
		public int MediumSpeed => Speeds[1];
		public int HighSpeed => Speeds[0];
#nullable enable
	}
	public class SceneDescriptor_ContainsAirGroundModelData_WithNormalUpDown3Speeds_AndHPMount : SceneDescriptor_ContainsAirGroundModelData, IContainsGreatPerfectAndHPMount
	{
#nullable disable
		public Nucleus.Models.Runtime.Animation FindGreatAnimation(ModelInstance model) => model.Data.FindAnimation(Great);
		public Nucleus.Models.Runtime.Animation FindPerfectAnimation(ModelInstance model) => model.Data.FindAnimation(Perfect);
		public BoneInstance GetHPMount(ModelInstance model) => model.FindBone(HPMount) ?? throw new KeyNotFoundException($"Missing mount bone '{HPMount}' for {GetType().Name}. Either the scene descriptor failed to add a 'hp' bone in the model, or failed to define a custom name override.");

		[JsonProperty("normal")] public SceneDescriptor_FormatSpeeds Normal;
		[JsonProperty("up")] public SceneDescriptor_FormatSpeeds Up;
		[JsonProperty("down")] public SceneDescriptor_FormatSpeeds Down;
		[JsonProperty("hp_mount")] public string HPMount = "hp";
		[JsonProperty("great")] public string Great;
		[JsonProperty("perfect")] public string Perfect;

		public string GetAnimationString(int speed, EntityEnterDirection direction, out double showtime, out float xoffset) {
			var speedIndex = speed switch {
				1 => 2,
				2 => 1,
				3 => 0,
				_ => throw new Exception("Invalid speed")
			};

			var formatSpeeds = direction switch {
				EntityEnterDirection.RightSide => Normal,
				EntityEnterDirection.TopDown => Down,
				EntityEnterDirection.BottomUp => Up,
				_ => throw new Exception("Invalid speed")
			};

			var frameSpeed = formatSpeeds.Speeds[speedIndex];
			showtime = frameSpeed / Model4System.REFERENCE_FPS;
			xoffset = formatSpeeds.XOffset;
			return string.Format(formatSpeeds.Format, frameSpeed);
		}
#nullable enable
	}

	public class SceneDescriptor_SmallEnemy : SceneDescriptor_ContainsAirGroundModelData_WithNormalUpDown3Speeds_AndHPMount;
	public class SceneDescriptor_MediumEnemy1 : SceneDescriptor_ContainsAirGroundModelData_WithNormalUpDown3Speeds_AndHPMount;
	public class SceneDescriptor_MediumEnemy2 : SceneDescriptor_ContainsAirGroundModelData_WithNormalUpDown3Speeds_AndHPMount;
	public class SceneDescriptor_LargeEnemy1 : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount;
	public class SceneDescriptor_LargeEnemy2 : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount;

	public class SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndUpsideDown : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation
	{
#nullable disable
		[JsonProperty("upsidedown_airmodel")] public string UpsideDownAirModel;
		[JsonProperty("upsidedown_groundmodel")] public string UpsideDownGroundModel;

		[JsonIgnore] public ModelData UpsideDownAirModelData;
		[JsonIgnore] public ModelData UpsideDownGroundModelData;

		public override void LoadModelData(Level level) {
			base.LoadModelData(level);
			UpsideDownAirModelData = level.Models.LoadModelFromFile("scene", UpsideDownAirModel);
			UpsideDownGroundModelData = level.Models.LoadModelFromFile("scene", UpsideDownGroundModel);
		}

		public virtual ModelData GetModelFromPathway(PathwaySide pathway, bool flipped) => pathway switch {
			PathwaySide.Top => flipped ? UpsideDownAirModelData : AirModelData,
			PathwaySide.Bottom => flipped ? UpsideDownGroundModelData : GroundModelData,
			_ => throw new Exception("Invalid pathway.")
		};
#nullable enable
	}
	public class SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndUpsideDown_AndHPMount : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount
	{
#nullable disable
		[JsonProperty("upsidedown_airmodel")] public string UpsideDownAirModel;
		[JsonProperty("upsidedown_groundmodel")] public string UpsideDownGroundModel;

		[JsonIgnore] public ModelData UpsideDownAirModelData;
		[JsonIgnore] public ModelData UpsideDownGroundModelData;

		public override void LoadModelData(Level level) {
			base.LoadModelData(level);
			UpsideDownAirModelData = level.Models.LoadModelFromFile("scene", UpsideDownAirModel);
			UpsideDownGroundModelData = level.Models.LoadModelFromFile("scene", UpsideDownGroundModel);
		}

		public virtual ModelData GetModelFromPathway(PathwaySide pathway, bool flipped) => pathway switch {
			PathwaySide.Top => flipped ? UpsideDownAirModelData : AirModelData,
			PathwaySide.Bottom => flipped ? UpsideDownGroundModelData : GroundModelData,
			_ => throw new Exception("Invalid pathway.")
		};
#nullable enable
	}

	public class SceneDescriptor_Hammer : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndUpsideDown;
	public class SceneDescriptor_Raider : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndUpsideDown_AndHPMount;
	public class SceneDescriptor_Ghost : SceneDescriptor_ContainsAirGroundModelData_With3SpeedsAndAnimation_AndHPMount;

	public class SceneDescriptor_Bonus
	{
#nullable disable
		[JsonProperty("model")] public string Model;
		[JsonProperty("air")] public string AirAnimation;
		[JsonProperty("ground")] public string GroundAnimation;
#nullable enable
		[JsonProperty("mount")] public string? MountAnimation;
#nullable disable
		[JsonProperty("out")] public string OutAnimation;

		[JsonIgnore] public ModelData ModelData;
		public void LoadModelData(Level level) {
			ModelData = level.Models.LoadModelFromFile("scene", Model);
		}
#nullable enable
	}

	public class SceneDescriptor_Score : SceneDescriptor_Bonus;
	public class SceneDescriptor_Heart : SceneDescriptor_Bonus;
	public class SceneDescriptor_BossStandby
	{
#nullable disable
		[JsonProperty("0")] public string Standby0;
		[JsonProperty("1")] public string Standby1;
		[JsonProperty("2")] public string Standby2;
#nullable enable
	}
	public class SceneDescriptor_BossAttack
	{
#nullable disable
		[JsonProperty("air")] public string Air;
		[JsonProperty("ground")] public string Ground;
#nullable enable

		public static implicit operator SceneDescriptor_BossAttack(string s) => new() { Air = s, Ground = s };
	}
	public class SceneDescriptor_BossAttacks
	{
#nullable disable
		[JsonProperty("1")] public SceneDescriptor_BossAttack Attack1;
		[JsonProperty("2")] public SceneDescriptor_BossAttack Attack2;
#nullable enable
	}
	public class SceneDescriptor_BossTransition
	{
		[JsonProperty("0")] public string? To0;
		[JsonProperty("1")] public string? To1;
		[JsonProperty("2")] public string? To2;
	}
	public class SceneDescriptor_BossTransitions
	{
#nullable disable
		[JsonProperty("0")] public SceneDescriptor_BossTransition From0;
		[JsonProperty("1")] public SceneDescriptor_BossTransition From1;
		[JsonProperty("2")] public SceneDescriptor_BossTransition From2;
#nullable enable
	}
	public class SceneDescriptor_BossNameSpeed {

		[JsonProperty("name")] public string Name;
		[JsonProperty("speed")] public float Speed;
	}
	public class SceneDescriptor_BossClose
	{
		[JsonProperty("fast")] public SceneDescriptor_BossNameSpeed AttackFast;
		[JsonProperty("slow")] public SceneDescriptor_BossNameSpeed AttackSlow;
	}
	public class SceneDescriptor_BossMulti
	{
		[JsonProperty("atk")] public SceneDescriptor_BossNameSpeed Attack;
		[JsonProperty("atk_end")] public string AttackEnd;
		[JsonProperty("hurt")] public string Hurt;
		[JsonProperty("hurt_end")] public string HurtEnd;
		[JsonProperty("atk_out")] public string AttackOut;
	}
	public class SceneDescriptor_Hitsounds
	{
#nullable disable
		[JsonProperty("punch")] public string Punch;

		[JsonIgnore] public Sound PunchSound;
#nullable enable
		public void Load(Level level) {
			PunchSound = level.Sounds.LoadSoundFromFile("scene", Punch);
			PunchSound.BindVolumeToConVar(AudioSettings.clonedash_hitsound_volume);
			Interlude.Spin();
		}
	}

	public void Initialize(Level level) {
		Interlude.Spin(submessage: "Initializing sounds...");
		AnnouncerLines.BeginSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.Begin); Interlude.Spin();
		AnnouncerLines.FeverSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.Fever); Interlude.Spin();
		AnnouncerLines.UnpauseSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.Unpause); Interlude.Spin();
		AnnouncerLines.FullComboSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.FullCombo); Interlude.Spin();

		AnnouncerLines.BeginSound.BindVolumeToConVar(AudioSettings.clonedash_voice_volume);
		AnnouncerLines.FeverSound.BindVolumeToConVar(AudioSettings.clonedash_voice_volume);
		AnnouncerLines.UnpauseSound.BindVolumeToConVar(AudioSettings.clonedash_voice_volume);
		AnnouncerLines.FullComboSound.BindVolumeToConVar(AudioSettings.clonedash_voice_volume);

		Hitsounds.Load(level); Interlude.Spin(submessage: "Initializing hitsounds...");

		Boss.LoadModelData(level);Interlude.Spin(submessage: "Initializing boss...");
		Sustains.LoadData(level);Interlude.Spin(submessage: "Initializing sustain models...");
		Gears.LoadModelData(level);Interlude.Spin(submessage: "Initializing gear models...");
		BossGears.LoadModelData(level);Interlude.Spin(submessage: "Initializing boss gear models...");
		Masher.LoadModelData(level);Interlude.Spin(submessage: "Initializing masher models...");
		DoubleEnemy.LoadModelData(level);Interlude.Spin(submessage: "Initializing double models...");

		BossEnemy1.LoadModelData(level);Interlude.Spin(submessage: "Initializing boss enemy models...");
		BossEnemy2.LoadModelData(level);Interlude.Spin(submessage: "Initializing boss enemy models...");
		BossEnemy3.LoadModelData(level);Interlude.Spin(submessage: "Initializing boss enemy models...");

		SmallEnemy.LoadModelData(level);Interlude.Spin(submessage: "Initializing small enemy models...");

		MediumEnemy1.LoadModelData(level);Interlude.Spin(submessage: "Initializing medium enemy models...");
		MediumEnemy2.LoadModelData(level);Interlude.Spin(submessage: "Initializing medium enemy models...");

		LargeEnemy1.LoadModelData(level);Interlude.Spin(submessage: "Initializing large enemy models...");
		LargeEnemy2.LoadModelData(level);Interlude.Spin(submessage: "Initializing large enemy models...");

		Hammer.LoadModelData(level);Interlude.Spin(submessage: "Initializing hammer models...");

		Raider.LoadModelData(level);Interlude.Spin(submessage: "Initializing raider models...");

		Ghost.LoadModelData(level);Interlude.Spin(submessage: "Initializing ghost models...");

		Score.LoadModelData(level);Interlude.Spin(submessage: "Initializing extra models...");
		Heart.LoadModelData(level);Interlude.Spin(submessage: "Initializing extra models...");
	}

	public void PlayBegin() => AnnouncerLines.BeginSound.Play(.8f);
	public void PlayFever() => AnnouncerLines.FeverSound.Play(.8f);
	public void PlayUnpause() => AnnouncerLines.UnpauseSound.Play(.8f);
	public void PlayFullCombo() => AnnouncerLines.FullComboSound.Play(.8f);
	public void PlayPunch(float pitch = 1) => Hitsounds.PunchSound.Play(.3f, pitch);

	public static SceneDescriptor? ParseFile(string filepath) => ParseFile<SceneDescriptor>(Filesystem.ReadAllText("scenes", filepath) ?? "", filepath);
	public static SceneDescriptor? ParseScene(string filename) => Filesystem.ReadAllText("scenes", filename, out var text) ? ParseFile<SceneDescriptor>(text, filename) : null;

	internal void MountToFilesystem() {
		if (Filename == null) throw new FileNotFoundException("SceneDescriptor.MountToFilesystem: Cannot mount the file, because Filename == null!");
		Filesystem.RemoveSearchPath("scene");

		// Find the search path that contains the scene descriptor.
		// TODO: Need to redo this! It doesn't really support zip files (which was the whole
		// point of the filesystem restructure!)
		var searchPath = Filesystem.FindSearchPath("scenes", $"{Filename}/scene.cdd");
		switch (searchPath) {
			case DiskSearchPath diskPath:
				Filesystem.AddTemporarySearchPath("scene", DiskSearchPath.Combine(searchPath, Filename));
				break;
		}
	}

	[JsonProperty("name")] public string Name;
	[JsonProperty("author")] public string Author;

	[JsonProperty("background_controller")] public string PathToBackgroundController;

	[JsonProperty("announcer")] public SceneDescriptor_Announcer AnnouncerLines;
	[JsonProperty("hitsounds")] public SceneDescriptor_Hitsounds Hitsounds;

	[JsonProperty("punch")] public string Punch;
	[JsonProperty("boss")] public SceneDescriptor_Boss Boss;
	[JsonProperty("sustains")] public SceneDescriptor_Sustains Sustains;
	[JsonProperty("gears")] public SceneDescriptor_Gears Gears;
	[JsonProperty("boss_gears")] public SceneDescriptor_BossGears BossGears;

	[JsonProperty("boss1")] public SceneDescriptor_BossEnemy1 BossEnemy1;
	[JsonProperty("boss2")] public SceneDescriptor_BossEnemy2 BossEnemy2;
	[JsonProperty("boss3")] public SceneDescriptor_BossEnemy3 BossEnemy3;

	[JsonProperty("masher")] public SceneDescriptor_MasherEnemy Masher;
	[JsonProperty("double")] public SceneDescriptor_DoubleEnemy DoubleEnemy;
	[JsonProperty("small")] public SceneDescriptor_SmallEnemy SmallEnemy;
	[JsonProperty("medium1")] public SceneDescriptor_MediumEnemy1 MediumEnemy1;
	[JsonProperty("medium2")] public SceneDescriptor_MediumEnemy2 MediumEnemy2;
	[JsonProperty("large1")] public SceneDescriptor_LargeEnemy1 LargeEnemy1;
	[JsonProperty("large2")] public SceneDescriptor_LargeEnemy2 LargeEnemy2;
	[JsonProperty("hammer")] public SceneDescriptor_Hammer Hammer;
	[JsonProperty("raider")] public SceneDescriptor_Raider Raider;
	[JsonProperty("ghost")] public SceneDescriptor_Ghost Ghost;
	[JsonProperty("heart")] public SceneDescriptor_Heart Heart;
	[JsonProperty("score")] public SceneDescriptor_Score Score;
}