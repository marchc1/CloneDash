using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using System.Collections.Concurrent;

namespace CD_UnityGenAssetLUT;

public record UnityAsset(string bundleName, long path);

internal class Program
{
	static void Main(string[] args) {
		var directory = Path.Combine(AppContext.BaseDirectory, "../../../../BuildAssets/universal/assets");
		if (!Directory.Exists(directory)) {
			Console.WriteLine("Can't find the Clone Dash build assets, exiting. Press any key to shut down");
			Console.ReadKey();
			return;
		}

		Console.WriteLine("This will take some time.");
		MuseDashCompatibility.LightInitialize();
		ConcurrentDictionary<string, UnityAsset> lookup = [];
		int total = MuseDashCompatibility.StreamingFiles.Length;
		int remaining = total;

		Thread t = new Thread(() => {
			Parallel.ForEach(MuseDashCompatibility.StreamingFiles, (file) => {
				if (file.StartsWith("account_assets")) return;

				//string filename = Regex.Match(Path.GetFileName(file), @"^(.*?(?=\.|_[0-9a-fA-F]{32}))").Groups[1].Value;
				string filename = Path.GetFileName(file);
				AssetsManager assets = new AssetsManager();
				assets.LoadFiles(file);
				var asset = assets.assetsFileList[0];
				var obj = asset.Objects.First(x => x is AssetBundle) as AssetBundle;
				if (obj == null) return;
				foreach (var container in obj.m_Container) {
					var pathID = container.Value.asset.m_PathID;
					lookup.TryAdd($"{container.Key}/{(asset.ObjectsDic[pathID] is NamedObject no ? no.m_Name : pathID)}", new(filename, pathID));
				}

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
		Console.WriteLine("Encoding.");

		using FileStream stream = File.OpenWrite(Path.Combine(directory, "mdlut.dat"));
		using BinaryWriter writer = new BinaryWriter(stream);

		writer.Write(DateTime.UtcNow.Ticks);
		writer.Write(lookup.Count);
		foreach (var kvp in lookup) {
			writer.Write(kvp.Key);
			writer.Write(kvp.Value.bundleName);
			writer.Write(kvp.Value.path);
		}

		Console.WriteLine("Done.");
	}
}
