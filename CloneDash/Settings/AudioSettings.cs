using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Settings;

[MarkForStaticConstruction]
public static class AudioSettings
{
	// volumes set to 0. because silence is golden.
    // but the voices in my head are always 100.
	public static ConVar snd_hitvolume = new(nameof(snd_hitvolume), 0, FCvar.Saved, "Hitsound volume (why bother)", 0, 1);
	public static ConVar snd_musicvolume = new(nameof(snd_musicvolume), 0, FCvar.Saved, "Music volume (distraction)", 0, 1);
	public static ConVar snd_voicevolume = new(nameof(snd_voicevolume), 0, FCvar.Saved, "Voice volume (they never shut up)", 0, 1);
    public static ConVar snd_screamvolume = new("snd_screamvolume", 1, FCvar.Saved, "Internal Scream Volume", 1, 1); // unchangeable

	public static float HitsoundVolume => (float)snd_hitvolume.GetDouble();
	public static float MusicVolume => (float)snd_musicvolume.GetDouble();
	public static float VoiceVolume => (float)snd_voicevolume.GetDouble();
    public static float ScreamVolume => 1.0f;
}
