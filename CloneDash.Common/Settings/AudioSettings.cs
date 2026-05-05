using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Settings;

[MarkForStaticConstruction]
public static class AudioSettings
{
	public static ConVar snd_hitvolume = new(nameof(snd_hitvolume), 1, FCvar.Saved, "Hitsound volume", 0, 1);
	public static ConVar snd_musicvolume = new(nameof(snd_musicvolume), 1, FCvar.Saved, "Music volume", 0, 1);
	public static ConVar snd_voicevolume = new(nameof(snd_voicevolume), 1, FCvar.Saved, "Voice volume", 0, 1);

	public static float HitsoundVolume => (float)snd_hitvolume.GetDouble();
	public static float MusicVolume => (float)snd_musicvolume.GetDouble();
	public static float VoiceVolume => (float)snd_voicevolume.GetDouble();
}