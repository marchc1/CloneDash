using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Modding.Descriptors;

public class SceneDescriptor : CloneDashDescriptor, IDisposable
{
	public SceneDescriptor() : base(CloneDashDescriptorType.Scene, "2") { }
	public class SceneDescriptor_Announcer
	{
		[JsonProperty("begin")] public string Begin;
		[JsonProperty("fever")] public string Fever;
		[JsonProperty("unpause")] public string Unpause;
		[JsonProperty("fullcombo")] public string FullCombo;

		public Sound BeginSound;
		public Sound FeverSound;
		public Sound UnpauseSound;
		public Sound FullComboSound;
	}
	public class SceneDescriptor_Boss
	{
		[JsonProperty("model")] public string Model;

		[JsonProperty("in")] public string In;
		[JsonProperty("out")] public string Out;
		[JsonProperty("hurt")] public string Hurt;

		[JsonProperty("standby")] public SceneDescriptor_BossStandby Standby;
		[JsonProperty("attacks")] public SceneDescriptor_BossAttacks Attacks;
		[JsonProperty("transitions")] public SceneDescriptor_BossTransitions Transitions;
		[JsonProperty("close")] public SceneDescriptor_BossClose Close;
		[JsonProperty("multi")] public SceneDescriptor_BossMulti Multi;
	}
	public class SceneDescriptor_BossStandby
	{
		[JsonProperty("0")] public string Standby0;
		[JsonProperty("1")] public string Standby1;
		[JsonProperty("2")] public string Standby2;
	}
	public class SceneDescriptor_BossAttack {
		[JsonProperty("air")] public string Air;
		[JsonProperty("road")] public string Road;

		public static implicit operator SceneDescriptor_BossAttack(string s) => new() { Air = s, Road = s };
	}
	public class SceneDescriptor_BossAttacks
	{
		[JsonProperty("1")] public SceneDescriptor_BossAttack Attack1;
		[JsonProperty("2")] public SceneDescriptor_BossAttack Attack2;
	}
	public class SceneDescriptor_BossTransition {
		[JsonProperty("0")] public string? To0;
		[JsonProperty("1")] public string? To1;
		[JsonProperty("2")] public string? To2;
	}
	public class SceneDescriptor_BossTransitions {
		[JsonProperty("0")] public SceneDescriptor_BossTransition From0;
		[JsonProperty("1")] public SceneDescriptor_BossTransition From1;
		[JsonProperty("2")] public SceneDescriptor_BossTransition From2;
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



	[JsonProperty("punch")] public string Punch;
	public Sound PunchSound;

	[JsonProperty("boss")] public SceneDescriptor_Boss Boss;

	public void Initialize(Level level) {
		var folderpath = Path.GetDirectoryName(Filename);
		AnnouncerLines.BeginSound = level.Sounds.LoadSoundFromFile(Path.Combine(folderpath, AnnouncerLines.Begin));
		AnnouncerLines.FeverSound = level.Sounds.LoadSoundFromFile(Path.Combine(folderpath, AnnouncerLines.Fever));
		AnnouncerLines.UnpauseSound = level.Sounds.LoadSoundFromFile(Path.Combine(folderpath, AnnouncerLines.Unpause));
		AnnouncerLines.FullComboSound = level.Sounds.LoadSoundFromFile(Path.Combine(folderpath, AnnouncerLines.FullCombo));
		PunchSound = level.Sounds.LoadSoundFromFile(Path.Combine(folderpath, Punch));
	}

	public void PlayBegin() => AnnouncerLines.BeginSound.Play(.8f);
	public void PlayFever() => AnnouncerLines.FeverSound.Play(.8f);
	public void PlayUnpause() => AnnouncerLines.UnpauseSound.Play(.8f);
	public void PlayFullCombo() => AnnouncerLines.FullComboSound.Play(.8f);
	public void PlayPunch() => PunchSound.Play(.3f);

	[JsonProperty("name")] public string Name;
	[JsonProperty("author")] public string Author;

	[JsonProperty("announcer")] public SceneDescriptor_Announcer AnnouncerLines;

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
}
