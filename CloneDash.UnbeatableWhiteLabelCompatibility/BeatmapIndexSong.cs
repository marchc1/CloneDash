using CloneDash.Common;
using CloneDash.Charts;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Songs;
using Nucleus.Common.Audio;
using System.Text.Json.Serialization;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class BeatmapIndexSong : ModDescriptor, ISong
{
	[JsonPropertyName("name")] public string Name { get; set; } = null!;
	[JsonPropertyName("stageScene")] public string StageScene { get; set; } = null!;
	[JsonPropertyName("beatmaps")] public BeatmapInfo[] Beatmaps { get; set; } = null!;

	public IAudioClip? PreviewClip;

	public SongMetadata FetchMetadata() {
		return new() {
			Name = Name,
			Author = "D-Cell Games"
		};
	}

	public IReadOnlyList<ISongChart> GetCharts() {
		return Beatmaps;
	}

	public SongCoverInfo GetCoverTexture() {
		return new() {
			// todo
		};
	}

	public IAudioClip? GetDemoAudio() {
		return PreviewClip;
	}

	public override string GetName() => Name;
	public override string GetID() => Name.ToLower();
	public override ICloneDashModProvider GetProvider() => ChartMod.Instance.GetProviderByID("unbeatable_whitelabel")!;

	public bool IsAsynchronouslyLoading() {
		return false;
	}

	public void WaitForAsynchronousLoad(OnAsynchronousLoadingCompleteFn callback) => throw new NotImplementedException();
}
