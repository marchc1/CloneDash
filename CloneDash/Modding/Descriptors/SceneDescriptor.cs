using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Modding.Descriptors;

public class SceneDescriptor : CloneDashDescriptor, IDisposable
{
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
	public SceneDescriptor() : base(CloneDashDescriptorType.Scene, "2") { }

	[JsonProperty("punch")] public string Punch;
	public Sound PunchSound;

	public void Initialize(Level level) {
		var folderpath = Path.GetDirectoryName(Filepath);
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

	public static SceneDescriptor ParseFile(string filepath) => ParseFile<SceneDescriptor>(filepath);

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
}
