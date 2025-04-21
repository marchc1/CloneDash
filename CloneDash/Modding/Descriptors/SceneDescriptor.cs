using Newtonsoft.Json;

using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Models.Runtime;
namespace CloneDash.Modding.Descriptors;

public class SceneDescriptor : CloneDashDescriptor, IDisposable
{
#nullable disable
	public SceneDescriptor() : base(CloneDashDescriptorType.Scene, "2") { }
#nullable enable
	public abstract class SceneDescriptor_ContainsOneModelData {
#nullable disable
		[JsonProperty("model")] public string Model;

		[JsonIgnore] public ModelData ModelData;
#nullable enable
		public void LoadModelData(Level level) {
			ModelData = level.Models.LoadModelFromFile("scene", Model);
		}
	}
	public abstract class SceneDescriptor_ContainsAirGroundModelData {
#nullable disable
		[JsonProperty("airmodel")] public string AirModel;
		[JsonProperty("groundmodel")] public string GroundModel;

		[JsonIgnore] public ModelData AirModelData;
		[JsonIgnore] public ModelData GroundModelData;
#nullable enable
		public void LoadModelData(Level level) {
			AirModelData = level.Models.LoadModelFromFile("scene", AirModel);
			GroundModelData = level.Models.LoadModelFromFile("scene", GroundModel);
		}

		public ModelData GetModelFromPathway(PathwaySide pathway) 
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
	public class SceneDescriptor_Sustains;
	public class SceneDescriptor_Saws;
	public class SceneDescriptor_MasherEnemy : SceneDescriptor_ContainsOneModelData {
#nullable disable
		public class __InAnimations {
			[JsonProperty("format")] public string Format;
			[JsonProperty("down")] public int[] DownSpeeds;
			[JsonProperty("normal")] public int[] NormalSpeeds;
		}
		public class __MissAnimations {
			[JsonProperty("format")] public string Format;
			[JsonProperty("normal")] public int[] NormalSpeeds;
		}
		public class __CompleteAnimations {
			[JsonProperty("great")] public string Great;
			[JsonProperty("perfect")] public string Perfect;
		}

		[JsonProperty("hurt")] public Descriptor_MultiAnimationClass Hurt;
		[JsonProperty("in")] public __InAnimations InAnimations;
		[JsonProperty("miss")] public __MissAnimations MissAnimations;
		[JsonProperty("complete")] public __CompleteAnimations CompleteAnimations;
#nullable enable
	}
	public class SceneDescriptor_DoubleEnemy;
	public class SceneDescriptor_BossEnemy1 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_BossEnemy2 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_BossEnemy3 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_SmallEnemy : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_MediumEnemy1 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_MediumEnemy2 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_LargeEnemy1 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_LargeEnemy2 : SceneDescriptor_ContainsAirGroundModelData;
	public class SceneDescriptor_Hammer;
	public class SceneDescriptor_Raider;
	public class SceneDescriptor_Ghost;
	public class SceneDescriptor_Note;
	public class SceneDescriptor_Heart;
	public class SceneDescriptor_BossStandby
	{
#nullable disable
		[JsonProperty("0")] public string Standby0;
		[JsonProperty("1")] public string Standby1;
		[JsonProperty("2")] public string Standby2;
#nullable enable
	}
	public class SceneDescriptor_BossAttack {
#nullable disable
		[JsonProperty("air")] public string Air;
		[JsonProperty("road")] public string Road;
#nullable enable

		public static implicit operator SceneDescriptor_BossAttack(string s) => new() { Air = s, Road = s };
	}
	public class SceneDescriptor_BossAttacks
	{
#nullable disable
		[JsonProperty("1")] public SceneDescriptor_BossAttack Attack1;
		[JsonProperty("2")] public SceneDescriptor_BossAttack Attack2;
#nullable enable
	}
	public class SceneDescriptor_BossTransition {
		[JsonProperty("0")] public string? To0;
		[JsonProperty("1")] public string? To1;
		[JsonProperty("2")] public string? To2;
	}
	public class SceneDescriptor_BossTransitions {
#nullable disable
		[JsonProperty("0")] public SceneDescriptor_BossTransition From0;
		[JsonProperty("1")] public SceneDescriptor_BossTransition From1;
		[JsonProperty("2")] public SceneDescriptor_BossTransition From2;
#nullable enable
	}
	public class SceneDescriptor_BossClose
	{
		[JsonProperty("24")] public string? Attack24;
		[JsonProperty("48")] public string? Attack48;
	}
	public class SceneDescriptor_BossMulti
	{
		[JsonProperty("atk")] public string? Attack;
		[JsonProperty("atk_end")] public string? AttackEnd;
		[JsonProperty("hurt")] public string? Hurt;
		[JsonProperty("hurt_end")] public string? HurtEnd;
		[JsonProperty("atk_out")] public string? AttackOut;
	}

	public Sound PunchSound;
	public void Initialize(Level level) {
		AnnouncerLines.BeginSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.Begin);
		AnnouncerLines.FeverSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.Fever);
		AnnouncerLines.UnpauseSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.Unpause);
		AnnouncerLines.FullComboSound = level.Sounds.LoadSoundFromFile("scene", AnnouncerLines.FullCombo);

		PunchSound = level.Sounds.LoadSoundFromFile("scene", Punch);

		Boss.LoadModelData(level);
		Masher.LoadModelData(level);

		BossEnemy1.LoadModelData(level);
		BossEnemy2.LoadModelData(level);
		BossEnemy3.LoadModelData(level);
		SmallEnemy.LoadModelData(level);
		MediumEnemy1.LoadModelData(level);
		MediumEnemy2.LoadModelData(level);
		LargeEnemy1.LoadModelData(level);
		LargeEnemy2.LoadModelData(level);
	}

	public void PlayBegin() => AnnouncerLines.BeginSound.Play(.8f);
	public void PlayFever() => AnnouncerLines.FeverSound.Play(.8f);
	public void PlayUnpause() => AnnouncerLines.UnpauseSound.Play(.8f);
	public void PlayFullCombo() => AnnouncerLines.FullComboSound.Play(.8f);
	public void PlayPunch() => PunchSound.Play(.3f);

	public static SceneDescriptor? ParseFile(string filepath) => ParseFile<SceneDescriptor>(Filesystem.ReadAllText("scenes", filepath) ?? "", filepath);

	private bool disposedValue;
	protected virtual void Dispose(bool disposing) {
		if (!disposedValue) {
			if (disposing) {
				// TODO: dispose managed state (managed objects)
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			disposedValue = true;
		}
	}

	~SceneDescriptor() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

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

	[JsonProperty("announcer")] public SceneDescriptor_Announcer AnnouncerLines;

	[JsonProperty("punch")] public string Punch;
	[JsonProperty("boss")] public SceneDescriptor_Boss Boss;
	[JsonProperty("boss1")] public SceneDescriptor_BossEnemy1 BossEnemy1;
	[JsonProperty("boss2")] public SceneDescriptor_BossEnemy2 BossEnemy2;
	[JsonProperty("boss3")] public SceneDescriptor_BossEnemy3 BossEnemy3;

	[JsonProperty("sustains")] public SceneDescriptor_Sustains Sustains;
	[JsonProperty("saw")] public SceneDescriptor_Saws Saws;
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
	[JsonProperty("note")] public SceneDescriptor_Note Note;
	[JsonProperty("heart")] public SceneDescriptor_Heart Heart;
}
