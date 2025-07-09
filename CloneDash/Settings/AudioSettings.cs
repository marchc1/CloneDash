using Nucleus;

namespace CloneDash.Settings;

[MarkForStaticConstruction]
public static class AudioSettings
{
	public static ConVar snd_hitvolume = ConVar.Register(nameof(snd_hitvolume), 1, ConsoleFlags.Saved, "Hitsound volume", 0, 1);
	public static ConVar snd_musicvolume = ConVar.Register(nameof(snd_musicvolume), 1, ConsoleFlags.Saved, "Music volume", 0, 1);
	public static ConVar snd_voicevolume = ConVar.Register(nameof(snd_voicevolume), 1, ConsoleFlags.Saved, "Voice volume", 0, 1);

	public static float HitsoundVolume => (float)snd_hitvolume.GetDouble();
	public static float MusicVolume => (float)snd_musicvolume.GetDouble();
	public static float VoiceVolume => (float)snd_voicevolume.GetDouble();
}