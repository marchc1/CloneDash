using CloneDash.Common.Compatibility;
using CloneDash.Common.Interludes;
using CloneDash.Compatibility.Unity;

namespace CloneDash.Compatibility.MuseDash;

/// <summary>
/// Provides interlude textures from Muse Dash.
/// </summary>
public class MuseDash1InterludeProvider() : ModProvider<IInterludeDescriptor>("Muse Dash 1", "musedash1"), IInterludeProvider
{
	const string INTERLUDE_PREFIX = "loadinginterlude_assets_interlude_";

	string[]? interludeFiles;
	string[] GetInterludeFiles() => interludeFiles ??= UnityAssetUtils.GetAllFiles(MuseDash1Compatibility.StreamingFiles, INTERLUDE_PREFIX, regex: true);

	static ReadOnlySpan<char> GetInterludeID(string file) {
		ReadOnlySpan<char> name = Path.GetFileNameWithoutExtension(file.AsSpan());
		return name[INTERLUDE_PREFIX.Length..name.LastIndexOf('_')];
	}

	public override bool IsEnabled() => MuseDash1Compatibility.StreamingFiles != null;

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var file in GetInterludeFiles())
			yield return GetInterludeID(file).ToString();
	}

	public override IInterludeDescriptor? FindByID(ReadOnlySpan<char> id) {
		if (!IsEnabled())
			return null;

		foreach (var file in GetInterludeFiles())
			if (id.Equals(GetInterludeID(file), StringComparison.Ordinal))
				return new MuseDash1InterludeDescriptor(id.ToString(), file, this);

		return null;
	}
}
