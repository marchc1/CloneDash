using CloneDash.Compatibility.MuseDash;
using CloneDash.Common.Compatibility.Valve;

if (!OperatingSystem.IsMacOS()) { Console.WriteLine("macOS required"); return 2; }
var root = Path.Combine(Path.GetTempPath(), "clonedash-path-test-" + Guid.NewGuid());
try {
	var resources = Path.Combine(root, "MuseDash_Mac_Steam.app", "Contents", "Resources");
	var data = Path.Combine(resources, "Data");
	var assets = Path.Combine(data, "StreamingAssets", "aa", "StandaloneOSX");
	Directory.CreateDirectory(assets);
	File.WriteAllText(Path.Combine(assets, "fixture.bundle"), "fixture");
	SteamGames.Install = root;
	Check("bundle layout", MD1CompatLayerInitResult.OK);
	if (MuseDash1Compatibility.WhereIsMuseDashDataFolder != data) throw new Exception("Data folder mismatch");
	if (MuseDash1Compatibility.WhereIsMuseDashInstalled != resources) throw new Exception("Resources folder mismatch");
	if (MuseDash1Compatibility.BuildTarget != assets) throw new Exception("Build target mismatch");
	if (MuseDash1Compatibility.StandalonePlatform != "StandaloneOSX") throw new Exception("Platform mismatch");
	if (MuseDash1Compatibility.StreamingFiles.Length != 1) throw new Exception("Bundle enumeration mismatch");
	Directory.Delete(assets, true);
	Check("missing assets", MD1CompatLayerInitResult.StreamingAssetsNotFound);
	SteamGames.Install = null;
	Check("missing game", MD1CompatLayerInitResult.MuseDashNotInstalled);
	SteamGames.Steam = null;
	Check("missing Steam", MD1CompatLayerInitResult.SteamNotInstalled);
	Console.WriteLine("PASS: 4 macOS path cases");
	return 0;
} catch (Exception e) { Console.WriteLine("FAIL: " + e.Message); return 1; }
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

static void Check(string name, MD1CompatLayerInitResult expected) {
	var actual = MuseDash1Compatibility.Initialize();
	if (actual != expected) throw new Exception($"{name}: expected {expected}, got {actual}");
	Console.WriteLine($"PASS: {name} => {actual}");
}
namespace CloneDash.Common.Compatibility.Valve {
	public static class SteamGames {
		public static string? Steam = "fixture";
		public static string? Install;
		public static string? WhereIsSteamInstalled() => Steam;
		public static string? WhereIsGameInstalled(ulong id) => Install;
	}
}
namespace CloneDash.Compatibility.Valve { }
namespace CloneDash.Compatibility.MuseDash {
	public static partial class MuseDash1Compatibility {
		const ulong MUSEDASH_APPID = 774171;
		public static string? WhereIsMuseDashInstalled, WhereIsMuseDashDataFolder, StandalonePlatform, BuildTarget;
		public static string[] StreamingFiles = [];
		public static MD1CompatLayerInitResult Initialize() => INIT_OSX();
	}
}