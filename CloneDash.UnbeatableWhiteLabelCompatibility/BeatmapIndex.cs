using System.Text.Json.Serialization;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class BeatmapIndex
{
	[JsonPropertyName("m_Name")] public string Name { get; set; } = null!;
	[JsonPropertyName("songs")] public BeatmapIndexSong[] Songs { get; set; } = null!;
	public readonly Dictionary<string, BeatmapIndexSong> SongDict = [];
}
