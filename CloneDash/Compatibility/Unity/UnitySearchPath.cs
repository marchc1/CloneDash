using AssetStudio;

using Nucleus.Files;

using System.Diagnostics.CodeAnalysis;
using System.Text;

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

	public static string ReadUSPString(BinaryReader br, Encoding? encoding = null) {
		encoding = encoding ?? Encoding.UTF8;
		return br.ReadString();
	}

	public static void WriteUSPString(BinaryWriter bw, string text, Encoding? encoding = null) {
		encoding = encoding ?? Encoding.UTF8;
		bw.Write(text);
	}

	public static int ReadHeader(BinaryReader reader, out DateTime time) {
		time = new DateTime(reader.ReadInt64()).ToLocalTime();
		return reader.ReadInt32();
	}

	public static bool Read(BinaryReader reader, ref int i, int entries, out string container, out string name, out string bundle, out long pathID) {
		i++;
		if (i >= entries) {
			container = null;
			name = null;
			bundle = null;
			pathID = 0;
			return false;
		}

		container = UnitySearchPath.ReadUSPString(reader);
		name = UnitySearchPath.ReadUSPString(reader);
		bundle = UnitySearchPath.ReadUSPString(reader);
		pathID = reader.ReadInt64();
		return true;
	}

	public Dictionary<string, List<UnityFile>> NamedObjects = [];
	public Dictionary<long, UnityFile> PathIDObjects = [];

	public UnitySearchPath(string root, Stream serializedAssetBundleContents) {
		this.root = root;
		using BinaryReader reader = new(serializedAssetBundleContents);

		int entries = ReadHeader(reader, out DateTime time);
		int i = -1;
		while (Read(reader, ref i, entries, out string container, out string name, out string bundle, out long pathID)) {
			var parts = container.Split('/');
			UnityFolder folder = Root;
			for (int i2 = 0; i2 < parts.Length - 1; i2++) {
				folder = folder.Folder(parts[i2]);
				LookupAbsFolders[string.Join('/', parts[..(i2 + 1)])] = folder;
			}
			var file = folder.File(parts[parts.Length - 1]);
			file.PointsToBundle = bundle;
			file.PathID = pathID;

			LookupAbsFiles[container] = file;

			if (!string.IsNullOrEmpty(name)) {
				if (!NamedObjects.TryGetValue(name, out var list)) {
					list = [];
					NamedObjects[name] = list;
				}

				list.Add(file);
			}

			PathIDObjects[pathID] = file;
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

	private object l = new();
	protected override Stream? OnOpen(string path, FileAccess access, FileMode open) {
		lock (l) {
			var file = GetBundleNameFromFullPath(path);
			GetAssetBundle(file, out var manager);
			var asset = manager.assetsFileList[0].ObjectsDic[file.PathID];

			switch (asset) {
				case TextAsset ta: return new MemoryStream(ta.m_Script);
				default: throw new NotImplementedException($"No way to explicitly pull a stream out of a {asset.GetType().FullName}.");
			}
		}
	}

	public T LoadAsset<T>(string path) where T : AssetStudio.Object {
		var file = GetBundleNameFromFullPath(path);
		GetAssetBundle(file, out var manager);
		var asset = manager.assetsFileList[0].ObjectsDic[file.PathID];

		return (T)asset;
	}

	public T? FindAssetByName<T>(string name) where T : AssetStudio.Object {
		if (!NamedObjects.TryGetValue(name, out var files))
			return null;

		foreach (var file in files) {
			GetAssetBundle(file, out var manager);
			var asset = manager.assetsFileList[0].ObjectsDic[file.PathID];
			if (asset is T castObject)
				return castObject;
		}

		return null;
	}

	public T? FindAssetByPathID<T>(long pathID) where T : AssetStudio.Object {
		if (!PathIDObjects.TryGetValue(pathID, out var file))
			return null;

		GetAssetBundle(file, out var manager);
		var asset = manager.assetsFileList[0].ObjectsDic[file.PathID];
		if (asset is T castObject)
			return castObject;

		return null;
	}
}