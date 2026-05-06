using CloneDash.Compatibility.Unity;
using Texture2D = Raylib_cs.Texture2D;

namespace CloneDash.Compatibility.MuseDash;

/// <summary>
/// Provides interlude textures from Muse Dash.
/// </summary>
public class MuseDashInterludeProvider : InterludeTextureProvider
{
	static MuseDashInterlude[]? interludes;
	public override bool ShouldFlipTexture => true;
	private static bool ready = false;
	private static int setup() {
		if (ready && interludes != null) return interludes.Length;

		if (MuseDashCompatibility.WhereIsMuseDashInstalled == null) {
			return 0;
		}

		var interludesRaw = UnityAssetUtils.GetAllFiles(MuseDashCompatibility.StreamingFiles, "loadinginterlude_assets_interlude_", regex: true);
		interludes = new MuseDashInterlude[interludesRaw.Length];
		for (int i = 0; i < interludesRaw.Length; i++) {
			interludes[i] = new() {
				path = interludesRaw[i]
			};
		}
		ready = true;

		return interludes.Length;
	}

	public override int Count => setup();

	// Texture 2D used here because itll be loaded when Interlude is initialized.
	// And it will be destroyed immediately after
	public override bool Pick(int index, out Texture2D tex) {
		setup();
		if (!ready) {
			tex = default;
			return false;
		}
		var ttex = interludes?[index]?.LoadTexture();

		if (ttex.HasValue) {
			tex = ttex.Value;
			return true;
		}

		tex = default;
		return false;
	}
}