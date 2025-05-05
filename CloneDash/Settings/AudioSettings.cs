using Nucleus;

namespace CloneDash.Settings;

[MarkForStaticConstruction]
public static class AudioSettings
{
	private static ConVar clonedash_hitsound_volume = ConVar.Register(nameof(clonedash_hitsound_volume), 1, ConsoleFlags.Saved, 0, 1);
	private static ConVar clonedash_music_volume = ConVar.Register(nameof(clonedash_music_volume), 1, ConsoleFlags.Saved, 0, 1);
	private static ConVar clonedash_voice_volume = ConVar.Register(nameof(clonedash_voice_volume), 1, ConsoleFlags.Saved, 0, 1);

	public static float HitsoundVolume => (float)clonedash_hitsound_volume.GetDouble();
	public static float MusicVolume => (float)clonedash_music_volume.GetDouble();
	public static float VoiceVolume => (float)clonedash_voice_volume.GetDouble();
}