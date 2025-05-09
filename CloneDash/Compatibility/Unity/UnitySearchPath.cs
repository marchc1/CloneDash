using AssetStudio;
using Nucleus.Files;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CloneDash.Compatibility.Unity;

public class UnityAssetBundleInfo(string bundleName, long pathID)
{
	public string BundleName => bundleName;
	public long PathID => pathID;
}

public class UnityAssetLookups(int capacity) : Dictionary<string, UnityAssetBundleInfo>(capacity);

public class UnitySearchPath : SearchPath
{
	public DateTime WriteTime;
	private UnityAssetLookups lookups;
	private Dictionary<string, AssetsManager> assetsManagers = [];
	private string root;
	private HashSet<string> directories = [];
	private HashSet<string> files = [];
	private Dictionary<string, List<UnityAssetBundleInfo>> directoryFiles = [];

	private List<UnityAssetBundleInfo> getDirFiles(string directory) {
		if (!directoryFiles.TryGetValue(directory, out List<UnityAssetBundleInfo>? dirFiles)) {
			dirFiles = [];
			directoryFiles[directory] = dirFiles;
		}

		return dirFiles;
	}

	private UnityAssetBundleInfo RegisterFile(string filepath, UnityAssetBundleInfo file) {
		files.Add(filepath);
		var directory = Path.GetDirectoryName(filepath)!.Replace("\\", "/");
		getDirFiles(directory).Add(file);
		RegisterDirectory(directory);
		return file;
	}

	private void RegisterDirectory(string? directory) {
		if (string.IsNullOrWhiteSpace(directory)) return;
		directories.Add(directory);
		getDirFiles(directory);

		RegisterDirectory(Path.GetDirectoryName(directory)?.Replace("\\", "/"));
	}

	public UnitySearchPath(string root, Stream serializedAssetBundleContents) {
		this.root = root;
		using BinaryReader reader = new(serializedAssetBundleContents);
		WriteTime = new(reader.ReadInt64());
		int capacity = reader.ReadInt32();
		lookups = new(capacity);
		for (int i = 0; i < capacity; i++) {
			string container = reader.ReadString();
			string bundleName = reader.ReadString();
			long pathID = reader.ReadInt64();

			lookups[container] = RegisterFile(container, new(bundleName, pathID));
		}
	}

	public void GetAssetBundle(string bundleName, [NotNull] out AssetsManager manager) {
		if (assetsManagers.TryGetValue(bundleName, out manager))
			return;

		manager = new AssetsManager();
		manager.LoadFiles(Path.Combine(root, bundleName));
		assetsManagers.Add(bundleName, manager);
	}

	protected override bool CheckDirectory(string path, FileAccess? specificAccess = null, FileMode? specificMode = null)
		=> (specificAccess == null || specificAccess == FileAccess.Read)
		&& (specificMode == null || specificMode == FileMode.Open)
		&& directories.Contains(path);
	public override bool CheckFile(string path, FileAccess? specificAccess, FileMode? specificMode)
		=> (specificAccess == null || specificAccess == FileAccess.Read)
		&& (specificMode == null || specificMode == FileMode.Open)
		&& (files.Contains(path) || (directories.Contains(path) && directoryFiles[path].Count == 1));
	public string GetBundleNameFromFullPath(string path, out long pathID) {
		if (lookups.TryGetValue(path, out var bundleInfo)) {
			pathID = bundleInfo.PathID;
			return bundleInfo.BundleName;
		}

		if (directories.Contains(path)) {
			if (directoryFiles[path].Count != 1)
				throw new InvalidDataException("Tried to use a container as a filepath. This works when there's only one file available, but otherwise, you must specify a fully qualified container + filepath.");
			var obj = directoryFiles[path][0];
			pathID = obj.PathID;
			return obj.BundleName;
		}

		throw new FileNotFoundException(path);
	}
	public override IEnumerable<string> FindDirectories(string path, string searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}

	public override IEnumerable<string> FindFiles(string path, string searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}

	protected override Stream? OnOpen(string path, FileAccess access, FileMode open) {
		GetAssetBundle(GetBundleNameFromFullPath(path, out var pathID), out var manager);
		var asset = manager.assetsFileList[0].ObjectsDic[pathID];

		switch (asset) {
			case TextAsset ta: return new MemoryStream(ta.m_Script);
			default: throw new NotImplementedException($"No way to explicitly pull a stream out of a {asset.GetType().FullName}.");
		}
	}
}
