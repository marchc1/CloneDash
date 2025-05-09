using AssetStudio;
using Nucleus.Files;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CloneDash.Compatibility.Unity;

public class UnityFolder
{
	public string? Name;
	public UnityFolder? Parent;
	public Dictionary<string, UnityFolder> Directories = [];
	public Dictionary<string, UnityFile> Files = [];

	public UnityFolder Folder(string name) {
		if (!Directories.TryGetValue(name, out var folder)) {
			folder = new UnityFolder() {
				Parent = this
			};
			Directories.Add(name, folder);
		}

		return folder;
	}

	public UnityFile File(string name) {
		if (!Files.TryGetValue(name, out var file)) {
			file = new UnityFile() {
				Parent = this
			};
			Files.Add(name, file);
		}

		return file;
	}

	public UnityFile Single() => Files[Files.Keys.Single()];
}

public class UnityFile
{
	public string Name;
	public UnityFolder Parent;
	public string PointsToBundle;
	public long PathID;
}

public class UnitySearchPath : SearchPath
{
	public DateTime WriteTime;
	private UnityFolder Root = new();
	private Dictionary<string, AssetsManager> assetsManagers = [];
	private string root;

	public Dictionary<string, UnityFolder> LookupAbsFolders = [];
	public Dictionary<string, UnityFile> LookupAbsFiles = [];


	public UnitySearchPath(string root, Stream serializedAssetBundleContents) {
		this.root = root;
		using BinaryReader reader = new(serializedAssetBundleContents);
		WriteTime = new(reader.ReadInt64());
		int capacity = reader.ReadInt32();
		for (int i = 0; i < capacity; i++) {
			string container = reader.ReadString();
			string bundleName = reader.ReadString();
			long pathID = reader.ReadInt64();

			var parts = container.Split('/');
			UnityFolder folder = Root;
			for (int i2 = 0; i2 < parts.Length - 1; i2++) {
				folder = folder.Folder(parts[i2]);
				LookupAbsFolders[string.Join('/', parts[..(i2 + 1)])] = folder;
			}
			var file = folder.File(parts[parts.Length - 1]);
			file.PointsToBundle = bundleName;
			file.PathID = pathID;

			LookupAbsFiles[container] = file;
		}
	}

	public void GetAssetBundle(UnityFile file, [NotNull] out AssetsManager manager) {
		if (assetsManagers.TryGetValue(file.PointsToBundle, out manager))
			return;

		manager = new AssetsManager();
		manager.LoadFiles(Path.Combine(root, file.PointsToBundle));
		assetsManagers.Add(file.PointsToBundle, manager);
	}

	protected override bool CheckDirectory(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) => true;
	public override bool CheckFile(string path, FileAccess? specificAccess, FileMode? specificMode) => true;
	public UnityFile GetBundleNameFromFullPath(string path) {
		if (LookupAbsFiles.TryGetValue(path, out var bundleInfo)) return bundleInfo;
		if (LookupAbsFolders.TryGetValue(path, out var folder)) return folder.Single();

		// More expensive lookup method.
		// todo: cheapen this with local searching or something

		var tryFind = LookupAbsFiles.FirstOrDefault(x => x.Key.StartsWith(path));
		if (tryFind.Value == null) {
			var tryFind2 = LookupAbsFolders.FirstOrDefault(x => x.Key.StartsWith(path));
			if (tryFind2.Value != null)
				return tryFind2.Value.Single();
		}
		else return tryFind.Value;

		throw new FileNotFoundException(path);
	}

	public override IEnumerable<string> FindDirectories(string path, string searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}

	public override IEnumerable<string> FindFiles(string path, string searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}

	protected override Stream? OnOpen(string path, FileAccess access, FileMode open) {
		var file = GetBundleNameFromFullPath(path);
		GetAssetBundle(file, out var manager);
		var asset = manager.assetsFileList[0].ObjectsDic[file.PathID];

		switch (asset) {
			case TextAsset ta: return new MemoryStream(ta.m_Script);
			default: throw new NotImplementedException($"No way to explicitly pull a stream out of a {asset.GetType().FullName}.");
		}
	}

	public T LoadAsset<T>(string path) where T : AssetStudio.Object {
		var file = GetBundleNameFromFullPath(path);
		GetAssetBundle(file, out var manager);
		var asset = manager.assetsFileList[0].ObjectsDic[file.PathID];

		return (T)asset;
	}
}