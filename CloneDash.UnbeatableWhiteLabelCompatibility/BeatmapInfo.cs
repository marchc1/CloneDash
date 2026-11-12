using AssetStudio;
using CloneDash.Common;
using CloneDash.Common.Compatibility;
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

	public SongChartMetadata FetchMetadata() {
		return new() {
			ChartAuthors = "D-Cell Games",
			Difficulty = "1",
			DifficultyName = Difficulty,
			Color = new Nucleus.Common.Types.Color(115, 55, 55),
			GamemodeName = "UNBEATABLE [white label]",
		};
	}

	public IGamemodeDescriptor GetGamemode() {
		return ModStores.Get<GamemodeMod>().GetDescriptorByUID(UnbeatableWhiteLabelGamemode.UUID)!;
	}

	public Beatmap GetBeatmap() {
		if (Beatmap == null) {
			Beatmap beatmap = new();

			if (TextAsset.TryGet(out var textAsset)) {
				BeatmapParserEngine parser = new();
				parser.ReadBeatmap(Encoding.UTF8.GetString(textAsset.m_Script), ref beatmap);
			}

			Beatmap = beatmap;
		}

		return Beatmap;
	}

	void AddOneEntity(MuseDash1EntityType entityType, NoteInfo noteInfo, PathwaySet? pathwayOverride = null){
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
			Pathway = pathwayOverride ?? noteInfo.height switch { Height.Low => PathwayLayout.MuseDashBottom.AsSet, Height.Mid => PathwayLayout.MuseDashBottom.AsSet, Height.Top => PathwayLayout.MuseDashTop.AsSet, _ => PathwayLayout.MuseDashBottom.AsSet },
			Speed = noteInfo.speed switch {
				NoteInfo.NoteSpeed.Standard => 2,
				NoteInfo.NoteSpeed.Fast => 3,
				_ => 1
			},
			Score = 100,
			Variant = entityType != MuseDash1EntityType.Single ? EntityVariant.NotApplicable : EntityVariant.Medium1
		});
	}

	public object GetGamemodeData() => GetBeatmap();

	public MD1_GamemodeData GetMuseDash1Data() {
		if (GamemodeData == null) {
			GamemodeData = new MD1_GamemodeData();

			if (!TextAsset.TryGet(out _))
				return null!;

			GetBeatmap();

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
						AddOneEntity(entityType, noteInfo, PathwayLayout.MuseDashTop.AsSet);
						AddOneEntity(entityType, noteInfo, PathwayLayout.MuseDashBottom.AsSet);
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

	public IAudioClip GetAudioTrack() => UnbeatableWhiteLabelCompatibility.GetMasterClip(AudioClipName)!;
	public ChartTiming GetTiming() {
		List<TempoChange> tempoChanges = [];
		double beat = 0, lastTime = 0, lastBPM = 0;

		foreach (var timingPoint in GetBeatmap().timingPoints) {
			if (!timingPoint.uninherited || timingPoint.beatLength <= 0)
				continue;

			double time = timingPoint.time / 1000d;
			double bpm = 60000 / timingPoint.beatLength;
			if (lastBPM > 0)
				beat += (time - lastTime) * lastBPM / 240;
			else if (time > 0)
				tempoChanges.Add(new TempoChange(0, (int)Math.Round(-time * bpm / 240), bpm));

			lastTime = time;
			lastBPM = bpm;
			tempoChanges.Add(new TempoChange(time, (int)Math.Round(beat), lastBPM));
		}

		return new(tempoChanges, []);
	}

	public string AudioClipName = null!;

	public int GetRatingNumber() => 5; // todo
}
