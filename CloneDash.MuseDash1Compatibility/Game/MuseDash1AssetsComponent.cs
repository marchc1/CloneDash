using CloneDash.Common.Game;
using CloneDash.Compatibility.MuseDash;

namespace CloneDash.Game;

public class MuseDash1AssetsComponent : IDisposable
{
	public MuseDash1AssetsComponent(GameEventManager events) {
		events.Listen<LevelLoadedGameEvent>(UnloadAssets);
	}

	public void Dispose() {
		MuseDash1Compatibility.StreamingAssets?.UnloadAll();
	}

	void UnloadAssets(ref LevelLoadedGameEvent ev) {
		MuseDash1Compatibility.StreamingAssets?.UnloadAll();
	}
}