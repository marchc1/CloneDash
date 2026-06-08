using Org.Brotli.Dec;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AssetStudio;

public record struct ImportHelper_MergeSplitCache(string path, bool allDirectories);

public static class ImportHelper
{
	// Made this change to speed up AssetStudio loading since Muse Dash's StreamingAssets won't change during program lifetime
	static readonly Dictionary<ImportHelper_MergeSplitCache, string[]> MergeSplitCache = [];

	public static void MergeSplitAssets(string path, bool allDirectories = false) {
		ImportHelper_MergeSplitCache cacheLookup = new(path, allDirectories);
		string[] splitFiles;
		lock (MergeSplitCache) 
			if (!MergeSplitCache.TryGetValue(new(path, allDirectories), out splitFiles))
				splitFiles = MergeSplitCache[new(path, allDirectories)] = Directory.GetFiles(path, "*.split0", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		
		foreach (var splitFile in splitFiles) {
			var destFile = Path.GetFileNameWithoutExtension(splitFile);
			var destPath = Path.GetDirectoryName(splitFile);
			var destFull = Path.Combine(destPath, destFile);
			if (!File.Exists(destFull)) {
				var splitParts = Directory.GetFiles(destPath, destFile + ".split*");
				using (var destStream = File.Create(destFull)) {
					for (int i = 0; i < splitParts.Length; i++) {
						var splitPart = destFull + ".split" + i;
						using (var sourceStream = File.OpenRead(splitPart)) {
							sourceStream.CopyTo(destStream);
						}
					}
				}
			}
		}
	}

	public static string[] ProcessingSplitFiles(List<string> selectFile) {
		var splitFiles = selectFile.Where(x => x.Contains(".split"))
			.Select(x => Path.Combine(Path.GetDirectoryName(x), Path.GetFileNameWithoutExtension(x)))
			.Distinct()
			.ToList();
		selectFile.RemoveAll(x => x.Contains(".split"));
		foreach (var file in splitFiles) {
			if (File.Exists(file)) {
				selectFile.Add(file);
			}
		}
		return selectFile.Distinct().ToArray();
	}

	public static FileReader DecompressGZip(FileReader reader) {
		try {
			using (reader) {
				var stream = new MemoryStream();
				using (var gs = new GZipStream(reader.BaseStream, CompressionMode.Decompress)) {
					gs.CopyTo(stream);
				}
				stream.Position = 0;
				return new FileReader(reader.FullPath, stream);
			}
		}
		catch (System.Exception e) {
			Logger.Warning($"Error while decompressing Gzip file {reader.FullPath}\n{e}");
			reader.Dispose();
			return null;
		}
	}

	public static FileReader DecompressBrotli(FileReader reader) {
		try {
			using (reader) {
				var stream = new MemoryStream();
				using (var brotliStream = new BrotliInputStream(reader.BaseStream)) {
					brotliStream.CopyTo(stream);
				}
				stream.Position = 0;
				return new FileReader(reader.FullPath, stream);
			}
		}
		catch (System.Exception e) {
			Logger.Warning($"Error while decompressing Brotli file {reader.FullPath}\n{e}");
			reader.Dispose();
			return null;
		}
	}
}
