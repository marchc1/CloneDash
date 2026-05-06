using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;

namespace CloneDash.Common.Songs;

public delegate void OnAsynchronousLoadingCompleteFn(ISong self);

public struct SongMetadata{
	public string Name;
	public string Author;
}

public struct SongCoverInfo{
	public ITexture? Texture;
	public bool Flipped;
}

/// <summary>
/// Has a lowest difficulty, a highest difficulty, and can return a list of those difficulties.
/// </summary>
public interface IHasLowToHighDifficulties {
	int GetLowestDifficulty();
	int GetHighestDifficulty();
	int GetDifficultyCount();
	bool GetDifficulties(Span<int> difficulties);
}

/// <summary>
/// A song.
/// </summary>
public interface ISong : IUniquelyIdentifiableObject
{
	SongMetadata FetchMetadata(HumanLanguage desiredLanguage);

	/// <summary>
	/// Returns a read-only chart list
	/// </summary>
	IReadOnlyList<ISongChart> GetCharts();

	/// <summary>
	/// If true, the song is still loading its assets asynchronously, and DemoAudio/CoverTexture are not available yet.
	/// </summary>
	bool IsAsynchronouslyLoading();
	/// <summary>
	/// Registers a callback for when asynchronous loading is complete. If the song is synchronously loaded or already loaded
	/// its asynchronous assets, then this will immediately perform the callback. Otherwise, it will wait until:
	/// <br/> 1. The demo audio/cover texture have been fetched or are null for error-related reasons
	/// <br/> 2. The next game engine tick (after #1) to ensure same-thread guarantees of the callback.
	/// </summary>
	void WaitForAsynchronousLoad(OnAsynchronousLoadingCompleteFn callback);
	/// <summary>
	/// Gets the audio clip for the demo audio, if it was available. If asynchronously loading, this will be null and <see cref="IsAsynchronouslyLoading"/> will return true.
	/// </summary>
	IAudioClip? GetDemoAudio();
	/// <summary>
	/// Gets the cover texture for the demo audio, if it was available. If asynchronously loading, this will be null and <see cref="IsAsynchronouslyLoading"/> will return true.
	/// </summary>
	SongCoverInfo GetCoverTexture();
}
