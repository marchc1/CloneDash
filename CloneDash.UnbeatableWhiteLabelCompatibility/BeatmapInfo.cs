using AssetStudio;
using CloneDash.Common;
using CloneDash.Common.Data;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Unbeatable.Internal;
using Nucleus.Common.Audio;
using System.Text;
using System.Text.Json.Serialization;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class BeatmapInfo(BeatmapIndexSong song) : ISongChart
{
	[JsonPropertyName("textAsset")] public PPtr<TextAsset> TextAsset { get; set; } = null!;
	[JsonPropertyName("difficulty")] public string Difficulty { get; set; } = null!;

	public Beatmap Beatmap;

	MD1_GamemodeData? GamemodeData;

	public SongChartMetadata FetchMetadata(HumanLanguage desiredLanguage) {
		return new() {
			ChartAuthors = "D-Cell Games",
			Difficulty = "1",
			DifficultyName = Difficulty,
			Color = new Nucleus.Common.Types.Color(115, 55, 55),
			GamemodeName = "Muse Dash 1",
			ReturnedLanguage = HumanLanguage.English,
		};
	}

	public IGamemodeDescriptor GetGamemode() {
		return GamemodeMod.GetGamemode("gamemode/musedash1/standard")!;
	}

	void AddOneEntity(MuseDash1EntityType entityType, NoteInfo noteInfo, PathwaySide? pathwayOverride = null){
		GamemodeData!.Entities.Add(new() {
			Type = entityType,
			Damage = 30,
			EnterDirection = EntityEnterDirection.RightSide,
			Blood = false,
			Fever = 3,
			Flipped = false,
			HitTime = (noteInfo.time / 1000d),
			ShowTime = (noteInfo.time / 1000d) - noteInfo.speed switch {
				NoteInfo.NoteSpeed.Standard => 1,
				NoteInfo.NoteSpeed.Fast => 0.5,
				_ => 1
			},
			Length = entityType == MuseDash1EntityType.SustainBeam && noteInfo.hasEndTime ? (noteInfo.GetEndTime() / 1000d) - (noteInfo.time / 1000d) : 0,
			Pathway = pathwayOverride ?? noteInfo.height switch { Height.Low => PathwaySide.Bottom, Height.Mid => PathwaySide.Bottom, Height.Top => PathwaySide.Top, _ => PathwaySide.Bottom },
			Speed = noteInfo.speed switch {
				NoteInfo.NoteSpeed.Standard => 2,
				NoteInfo.NoteSpeed.Fast => 3,
				_ => 1
			},
			Score = 100,
			Variant = entityType != MuseDash1EntityType.Single ? EntityVariant.NotApplicable : EntityVariant.Medium1
		});
	}

	public object GetGamemodeData() {
		if (GamemodeData == null) {
			GamemodeData = new MD1_GamemodeData();

			if (!TextAsset.TryGet(out var textAsset))
				return null!;

			BeatmapParserEngine parser = new();
			Beatmap = new();
			parser.ReadBeatmap(Encoding.UTF8.GetString(textAsset.m_Script), ref Beatmap);

			GamemodeData.InitialScene = "scene/musedash1/scene_01";
			foreach (var tp in Beatmap.timingPoints) {
				if (tp.uninherited && tp.beatLength > 0) {
					double calculatedBpm = 60000.0 / tp.beatLength;

					GamemodeData.TempoChanges.Add(new TempoChange(
						time: tp.time,
						beat: tp.meter,
						bpm: calculatedBpm
					));
				}
			}
			foreach (var hitObjectInfo in Beatmap.hitObjects) {
				if (hitObjectInfo.IsFlip()) {
					continue;
				}
				else if (hitObjectInfo.IsAnyNote()) {
					NoteInfo noteInfo = new NoteInfo(hitObjectInfo, Side.Right);
					MuseDash1EntityType entityType = noteInfo.type switch {
						NoteType.Default => MuseDash1EntityType.Single,
						NoteType.Spam => MuseDash1EntityType.Masher,
						NoteType.Freestyle => MuseDash1EntityType.Masher,
						NoteType.Dodge => MuseDash1EntityType.Gear,
						NoteType.Double => MuseDash1EntityType.Double,
						NoteType.Hold => MuseDash1EntityType.SustainBeam,
						NoteType.Setpiece => MuseDash1EntityType.Raider,
						_ => 0
					};

					if (entityType == MuseDash1EntityType.Double) {
						AddOneEntity(entityType, noteInfo, PathwaySide.Top);
						AddOneEntity(entityType, noteInfo, PathwaySide.Bottom);
					}
					else
						AddOneEntity(entityType, noteInfo, null);
				}
				else if (hitObjectInfo.IsCommand()) {
					// todo
				}
			}
		}

		return GamemodeData;
	}

	public ISong GetSong() => song;

	public IAudioClip GetAudioTrack() => AudioClip;

	public IAudioClip AudioClip = null!;

	public int GetRatingNumber() => 5; // todo
}
