#define POST_VERIFY_LOOKUP

using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CD_UnityGenAssetLUT;

public record UnityAsset(string fullPath, string name, string bundleName, long path);

internal class Program
{
	public static bool IsNamed(PPtr<AssetStudio.Object> o, [NotNullWhen(true)] out string? name) {
		if (!o.TryGet(out var ro)) {
			name = null;
			return false;
		}

		return IsNamed(ro, out name);
	}
	public static bool IsNamed(AssetStudio.Object o, [NotNullWhen(true)] out string? name) {
		switch (o) {
			case GameObject go:
				name = go.m_Name;
				return true;
			case NamedObject no:
				name = no.m_Name;
				return true;
			default:
				name = null;
				return false;
		}
	}

	static void Main(string[] args) {
		var directoryExeAssets = Path.Combine(AppContext.BaseDirectory, "../../../../CloneDash/bin/Debug/net8.0/assets");
		var directoryBuildAssets = Path.Combine(AppContext.BaseDirectory, "../../../../BuildAssets/universal/assets");
		if (!Directory.Exists(directoryBuildAssets)) {
			Console.WriteLine("Can't find the Clone Dash build assets, exiting. Press any key to shut down");
			Console.ReadKey();
			return;
		}

		Console.WriteLine("This will take some time.");
		MuseDashCompatibility.LightInitialize();
		ConcurrentBag<UnityAsset> lookup = [];
		int total = MuseDashCompatibility.StreamingFiles.Length;
		int remaining = total;

		Thread t = new Thread(() => {
			Parallel.ForEach(MuseDashCompatibility.StreamingFiles, (file) => {
				if (file.StartsWith("account_assets")) goto ret;
				if (Path.GetExtension(file) != ".bundle") goto ret;

				//string filename = Regex.Match(Path.GetFileName(file), @"^(.*?(?=\.|_[0-9a-fA-F]{32}))").Groups[1].Value;
				string filename = Path.GetFileName(file);
				AssetsManager assets = new AssetsManager();
				assets.LoadFiles(file);
				var asset = assets.assetsFileList[0];
				var obj = asset.Objects.First(x => x is AssetBundle) as AssetBundle;
				if (obj == null) return;
				foreach (var container in obj.m_Container) {
					var pathID = container.Value.asset.m_PathID;
					if (pathID == 0) continue;
					lookup.Add(new(
						$"{container.Key}/{(IsNamed(asset.ObjectsDic[pathID], out var name1) ? name1 : pathID)}",
						(IsNamed(container.Value.asset, out var name2) ? name2 : ""),
						filename, pathID));
				}

			ret:
				Interlocked.Decrement(ref remaining);
			});
		});
		t.Start();

		int maxLen = 0;
		while (remaining > 0) {
			Console.SetCursorPosition(0, 3);
			var percentage = 1 - ((double)remaining / total);
			int progSize = 100;
			int progress = (int)(percentage * progSize);
			int progReverse = progSize - progress;
			var str = $"[{total - remaining}/{total} bundles] [{percentage:P5}] [{new string('/', progress)}{new string(' ', progReverse)}]";
			if (str.Length > maxLen) maxLen = str.Length;
			Console.Write(str.PadRight((maxLen + 1) - str.Length, ' '));
			Thread.Sleep(500);
		}

		Console.WriteLine();
		Console.WriteLine("Encoding...");
		{
			using (FileStream stream = File.OpenWrite(Path.Combine(directoryBuildAssets, "mdlut.dat"))) {
				using BinaryWriter writer = new BinaryWriter(stream);

				writer.Write(DateTime.UtcNow.Ticks);
				writer.Write(lookup.Count);
				foreach (var kvp in lookup) {
					UnitySearchPath.WriteUSPString(writer, kvp.fullPath);
					UnitySearchPath.WriteUSPString(writer, kvp.name);
					UnitySearchPath.WriteUSPString(writer, kvp.bundleName);
					writer.Write(kvp.path);
				}
			}
		}

#if POST_VERIFY_LOOKUP
		Console.WriteLine("Verifying...");
		{
			using (FileStream stream = File.OpenRead(Path.Combine(directoryBuildAssets, "mdlut.dat"))) {
				using BinaryReader reader = new BinaryReader(stream);

				var entries = UnitySearchPath.ReadHeader(reader, out DateTime time);
				Console.WriteLine($"Compile time: {time:f}");
				Console.WriteLine($"Entries: {entries}");

				int i = -1;
				while(UnitySearchPath.Read(reader, ref i, entries, out string container, out string name, out string bundle, out long pathID)) {
					if (!(container.StartsWith("Assets/") || container.StartsWith("Packages/")))
						throw new Exception("Invalid data.");
				}
			}
		}
#endif

		File.Copy(Path.Combine(directoryBuildAssets, "mdlut.dat"), Path.Combine(directoryExeAssets, "mdlut.dat"), true);
		Console.WriteLine("Done.");
	}
}
