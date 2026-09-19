using AssetStudio;
using CommunityToolkit.HighPerformance;
using K4os.Hash.xxHash;
using Nucleus.Common.FileSystem;
using Nucleus.Files;
using Nucleus.Util;
using SpirV;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloneDash.Compatibility.Unity;

#nullable disable
public class CatalogProviderObjectType
{
	public string m_AssemblyName;
	public string m_ClassName;
}

public class CatalogProviderData
{
	public string m_Id;
	public CatalogProviderObjectType m_ObjectType;
	public string m_Data;
}

public class CatalogResourceType
{
	public string m_AssemblyName;
	public string m_ClassName;
	public bool IsAssetBundleResource;
}

/// <summary>
/// A parsed catalog entry location
/// </summary>
public struct CatalogEntryLocation
{
	public string InternalId { get; set; }
	public string ProviderId { get; set; }
	public object DependencyKey { get; set; }
	public CatalogResourceType ResourceType { get; set; }
	public object PrimaryKey { get; set; }
	public int EntryIndex { get; set; }
	public int DataIndex { get; set; }
	public override string ToString() => $"[{ProviderId}] {InternalId} (key={PrimaryKey})";
}

public class AddressablesCatalog
{
	private readonly UnitySearchDirectory Root;

	public AddressablesCatalog() {
		Root = new(null, this);
	}

	public string m_LocatorId;
	public string m_BuildResultHash;
	public CatalogProviderData m_InstanceProviderData;
	public CatalogProviderData m_SceneProviderData;
	public CatalogProviderData[] m_ResourceProviderData;
	public string[] m_ProviderIds;
	public string[] m_InternalIds;

	[JsonIgnore] public byte[] m_KeyData;
	[JsonIgnore] public byte[] m_BucketData;
	[JsonIgnore] public byte[] m_EntryData;
	[JsonIgnore] public byte[] m_ExtraData;

	public CatalogResourceType[] m_resourceTypes;

	// [JsonIgnore] public readonly Dictionary<string, int> InternalIDReferences = [];
	// public int AddInternalIDRef(string name) {
	// 	if (!InternalIDReferences.TryGetValue(name, out int v))
	// 		v = 0;
	// 	InternalIDReferences[name] = v++;
	// 	return v;
	// }
	// public bool DoesInternalIDContainMultipleNames(string name) => InternalIDReferences.TryGetValue(name, out int v) && v > 1;

	[JsonIgnore] bool Decoded;
	[JsonIgnore] public readonly Dictionary<string, List<UnitySearchBase>> HashedAssetLookup = new(StringComparer.Ordinal);

	public void PushUnitySearchBase(ReadOnlySpan<char> hashedKey, UnitySearchBase searchBase) {
		if (!HashedAssetLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
			return;
		if (!lookup.TryGetValue(hashedKey, out var list))
			list = lookup[hashedKey] = new(64);

		list.Add(searchBase);
	}


#nullable enable
	public UnitySearchBase? Search(ReadOnlySpan<char> forWhat) {
		if (!HashedAssetLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
			return null;
		if (!lookup.TryGetValue(forWhat, out var list))
			return null;
		return list.FirstOrDefault();
	}
	public IReadOnlyList<UnitySearchBase> SearchAll(ReadOnlySpan<char> forWhat) {
		if (!HashedAssetLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
			return [];
		if (!lookup.TryGetValue(forWhat, out var list))
			return [];
		return list;
	}
	public UnitySearchBase? Search<T>(ReadOnlySpan<char> forWhat) {
		if (!HashedAssetLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
			return null;
		if (!lookup.TryGetValue(forWhat, out var list))
			return null;
		string expectedClassName = $"UnityEngine.{typeof(T).Name}";
		return list.FirstOrDefault(x => x is UnitySearchAsset asset && asset.Type.m_ClassName == expectedClassName);
	}
#nullable disable

	private const byte kAsciiString = 0;
	private const byte kUnicodeString = 1;
	private const byte kUInt16 = 2;
	private const byte kUInt32 = 3;
	private const byte kInt32 = 4;
	private const byte kHash128 = 5;
	private const byte kType = 6;
	private const byte kJsonObject = 7;

	private const int kBytesPerEntry = 28;

	ref struct ContainerParser(ReadOnlySpan<char> containerName)
	{
		ReadOnlySpan<char> work = containerName;

		public bool TryPiece(out ReadOnlySpan<char> piece, out bool last) {
			piece = default;
			last = false;
			if (work.Length == 0)
				return false;

			int loc = work.IndexOf('/');
			if (loc == -1) {
				piece = work;
				work = default;
				last = true;
				return true;
			}
			else {
				piece = work[..loc];
				work = work[(loc + 1)..];
				last = false;
				return true;
			}
		}
	}

	Dictionary<object, List<string>> DependencyKeyToBundleFiles = null!;

	public List<string> ResolveBundleFiles(object dependencyKey) {
		if (dependencyKey == null)
			throw new FileNotFoundException("Null dependency key");

		// Try direct lookup first
		if (DependencyKeyToBundleFiles.TryGetValue(dependencyKey, out var files))
			return files;

		// If the key is a string that looks like a number, try numeric types
		if (dependencyKey is string s) {
			if (int.TryParse(s, out int intVal) && DependencyKeyToBundleFiles.TryGetValue(intVal, out files))
				return files;
			if (uint.TryParse(s, out uint uintVal) && DependencyKeyToBundleFiles.TryGetValue(uintVal, out files))
				return files;
		}

		// If the key is numeric, try its string representation
		if (dependencyKey is int or uint or ushort) {
			string strVal = dependencyKey.ToString()!;
			if (DependencyKeyToBundleFiles.TryGetValue(strVal, out files))
				return files;
		}

		throw new FileNotFoundException($"Could not resolve dependency key '{dependencyKey}' (type: {dependencyKey.GetType().Name}) to bundle files.");
	}

	private static ReadOnlySpan<char> ExtractBundleFileName(ReadOnlySpan<char> incInternalId, Span<char> internalIdWrite) {
		if (incInternalId.IsEmpty)
			return default;

		internalIdWrite = internalIdWrite[..incInternalId.Length];
		incInternalId.Replace(internalIdWrite, '\\', Path.DirectorySeparatorChar);
		ReadOnlySpan<char> internalId = incInternalId;

		// Handle the {UnityEngine.AddressableAssets.Addressables.RuntimePath}/Platform/bundle.bundle format
		int prefixEnd = internalId.IndexOf('}');
		if (prefixEnd >= 0) {
			// Skip past the closing brace and the following slash
			ReadOnlySpan<char> afterPrefix = internalId[(prefixEnd + 1)..];
			return Path.GetFileName(afterPrefix);
		}

		// just get the filename
		return Path.GetFileName(internalId);
	}

	public void Decode() {
		if (Decoded)
			return;

		Span<object> keys = ReadKeys();
		CatalogEntryLocation[] allEntries = ReadEntries(keys);
		Dictionary<object, List<EntryAccessor>> locations = ReadBuckets(allEntries);
		// foreach (var entry in AllEntries)
		// 	AddInternalIDRef(entry.InternalId);
		{
			ConcurrentDictionary<object, ConcurrentQueue<UtlSymbol>> dictBuild = [];
			Parallel.ForEach(locations, kvp => {
				Span<char> internalIdWrite = stackalloc char[4096];
				foreach (var loc in kvp.Value) {
					ref CatalogEntryLocation entry = ref loc.Get();
					if (!(entry.ResourceType?.IsAssetBundleResource ?? false))
						continue;

					ReadOnlySpan<char> bundleFileName = ExtractBundleFileName(entry.InternalId, internalIdWrite);
					if (!bundleFileName.IsEmpty) {
						var list = dictBuild.GetOrAdd(kvp.Key, _ => new());
						UtlSymbol bundleFileSymbol = new(bundleFileName);
						if (!list.Contains(bundleFileSymbol))
							list.Enqueue(bundleFileSymbol);
					}
				}
			});

			Span<char> internalIdWrite = stackalloc char[4096];
			for (int i = 0, c = allEntries.Length; i < c; i++) {
				ref CatalogEntryLocation entry = ref allEntries[i];
				if (!(entry.ResourceType?.IsAssetBundleResource ?? false))
					continue;
				if (entry.PrimaryKey == null)
					continue;

				ReadOnlySpan<char> bundleFileName = ExtractBundleFileName(entry.InternalId, internalIdWrite);
				if (!bundleFileName.IsEmpty) {
					var list = dictBuild.GetOrAdd(entry.PrimaryKey, _ => new());
					UtlSymbol bundleFileSymbol = new(bundleFileName);
					if (!list.Contains(bundleFileSymbol))
						list.Enqueue(bundleFileSymbol);
				}
			}

			DependencyKeyToBundleFiles = dictBuild.Select(static kvp => new KeyValuePair<object, List<string>>(kvp.Key, [.. kvp.Value.Select(static x => x.String())])).ToDictionary();
		}

		Span<char> fullName = stackalloc char[8192];
		for (int i = 0, c = allEntries.Length; i < c; i++) {
			ref CatalogEntryLocation entry = ref allEntries[i];
			if (entry.ResourceType?.IsAssetBundleResource ?? false)
				continue;

			if (entry.InternalId == null || entry.DependencyKey == null)
				continue;

			if (!DependencyKeyToBundleFiles.ContainsKey(entry.DependencyKey))
				continue;

			ContainerParser parser = new(entry.InternalId);
			UnitySearchDirectory dir = Root;
			while (parser.TryPiece(out ReadOnlySpan<char> piece, out bool last))
				dir = dir.GetOrCreateDirectory(piece);

			UnitySearchAsset asset = dir.CreateFile(entry.PrimaryKey, entry.DependencyKey, entry.ResourceType);
			if (asset != null) {
				ReadOnlySpan<char> fullyQualifiedPath = dir.FullyQualifiedPath;
				ReadOnlySpan<char> assetName = asset.Name;
				int fullyQualifiedPathLength = fullyQualifiedPath.Length;

				fullyQualifiedPath.CopyTo(fullName);
				assetName.CopyTo(fullName[fullyQualifiedPathLength..]);

				PushUnitySearchBase(fullName[..(fullyQualifiedPathLength + assetName.Length)], asset);          // Container/Name
				PushUnitySearchBase(assetName, asset);        // PrimaryKey

				// Also index by just the filename portion of the PrimaryKey,
				// so callers using short names (e.g. "s01_arrow") can still find
				// assets whose address includes a folder prefix (e.g. "AtlasH/s01_arrow")
				// TODO: Review if we should keep this behavior? It's what AssetStudioGUI reports...
				// we'll see.
				int lastSlash = assetName.LastIndexOf('/');
				if (lastSlash >= 0) {
					ReadOnlySpan<char> shortName = assetName[(lastSlash + 1)..];
					PushUnitySearchBase(shortName, asset);
				}
			}
		}
		Decoded = true;

		m_KeyData = null;
		m_BucketData = null;
		m_EntryData = null;
		m_ExtraData = null;
	}

	private object[] ReadKeys() {
		int offset = 0;
		int keyCount = ReadInt32(m_KeyData, ref offset);
		var keys = new object[keyCount];

		for (int i = 0; i < keyCount; i++)
			keys[i] = ReadKey(m_KeyData, ref offset);

		return keys;
	}

	private object ReadKey(byte[] data, ref int offset) {
		byte typeCode = data[offset++];

		switch (typeCode) {
			case kAsciiString: {
					int len = ReadInt32(data, ref offset);
					string s = Encoding.ASCII.GetString(data, offset, len);
					offset += len;
					return s;
				}
			case kUnicodeString: {
					int len = ReadInt32(data, ref offset);
					string s = Encoding.Unicode.GetString(data, offset, len);
					offset += len;
					return s;
				}
			case kUInt16: {
					ushort v = (ushort)(data[offset] | (data[offset + 1] << 8));
					offset += 2;
					return v;
				}
			case kUInt32: {
					uint v = ReadUInt32(data, ref offset);
					return v;
				}
			case kInt32: {
					int v = ReadInt32(data, ref offset);
					return v;
				}
			case kHash128: {
					int len = ReadInt32(data, ref offset);
					string hash = Encoding.ASCII.GetString(data, offset, len);
					offset += len;
					return hash;
				}
			case kType: {
					int len = ReadInt32(data, ref offset);
					string typeName = Encoding.ASCII.GetString(data, offset, len);
					offset += len;
					return typeName;
				}
			case kJsonObject: {
					int assemblyLen = ReadInt32(data, ref offset);
					string assemblyName = Encoding.ASCII.GetString(data, offset, assemblyLen);
					offset += assemblyLen;

					int jsonLen = ReadInt32(data, ref offset);
					string jsonStr = Encoding.ASCII.GetString(data, offset, jsonLen);
					offset += jsonLen;

					return $"{assemblyName}:{jsonStr}";
				}
			default:
				throw new InvalidDataException($"Unknown key type code: {typeCode} at offset {offset - 1}");
		}
	}

	private CatalogEntryLocation[] ReadEntries(Span<object> keys) {
		int offset = 0;
		int entryCount = ReadInt32(m_EntryData, ref offset);
		var entries = new CatalogEntryLocation[entryCount];

		for (int i = 0; i < entryCount; i++) {
			int internalIdIdx = ReadInt32(m_EntryData, ref offset);
			int providerIdx = ReadInt32(m_EntryData, ref offset);
			int depKeyIdx = ReadInt32(m_EntryData, ref offset);
			int depHash = ReadInt32(m_EntryData, ref offset);
			int dataIdx = ReadInt32(m_EntryData, ref offset);
			int primaryKeyIdx = ReadInt32(m_EntryData, ref offset);
			int resourceTypeIdx = ReadInt32(m_EntryData, ref offset);

			var entry = new CatalogEntryLocation {
				InternalId = (internalIdIdx >= 0 && internalIdIdx < m_InternalIds.Length)
					? m_InternalIds[internalIdIdx]
					: null,
				ProviderId = (providerIdx >= 0 && providerIdx < m_ProviderIds.Length)
					? m_ProviderIds[providerIdx]
					: null,
				DependencyKey = (depKeyIdx >= 0 && depKeyIdx < keys.Length)
					? keys[depKeyIdx]
					: null,
				ResourceType = (resourceTypeIdx >= 0 && m_resourceTypes != null && resourceTypeIdx < m_resourceTypes.Length)
					? m_resourceTypes[resourceTypeIdx]
					: null,
				PrimaryKey = (primaryKeyIdx >= 0 && primaryKeyIdx < keys.Length)
					? keys[primaryKeyIdx]
					: null,
				EntryIndex = i,
				DataIndex = dataIdx
			};

			entries[i] = entry;
		}

		// These are no longer needed, so let garbage collection take care of these fields.
		m_InternalIds = null!;
		m_ProviderIds = null!;
		m_resourceTypes = null!;

		return entries;
	}

	struct EntryAccessor(CatalogEntryLocation[] entries, int index)
	{
		public ref CatalogEntryLocation Get() => ref entries[index];
	}

	private Dictionary<object, List<EntryAccessor>> ReadBuckets(CatalogEntryLocation[] entries) {
		int offset = 0;
		int bucketCount = ReadInt32(m_BucketData, ref offset);

		var locations = new Dictionary<object, List<EntryAccessor>>(bucketCount);

		for (int i = 0; i < bucketCount; i++) {
			int keyDataOffset = ReadInt32(m_BucketData, ref offset);

			int entryCount = ReadInt32(m_BucketData, ref offset);

			var entryIndices = new int[entryCount];
			for (int e = 0; e < entryCount; e++) {
				entryIndices[e] = ReadInt32(m_BucketData, ref offset);
			}

			int keyReadOffset = keyDataOffset;
			object key;
			try {
				key = ReadKey(m_KeyData, ref keyReadOffset);
			}
			catch {
				continue;
			}

			List<EntryAccessor> entryList;
			if (locations.TryGetValue(key, out entryList))
				entryList.EnsureCapacity(entryList.Count + entries.Length);
			else
				entryList = locations[key] = new(entryIndices.Length);

			foreach (int idx in entryIndices)
				if (idx >= 0 && idx < entries.Length)
					entryList.Add(new(entries, idx));
		}

		return locations;
	}

	private static int ReadInt32(byte[] data, ref int offset) {
		int value = data[offset]
			| (data[offset + 1] << 8)
			| (data[offset + 2] << 16)
			| (data[offset + 3] << 24);
		offset += 4;
		return value;
	}

	private static uint ReadUInt32(byte[] data, ref int offset) {
		uint value = (uint)(data[offset]
			| (data[offset + 1] << 8)
			| (data[offset + 2] << 16)
			| (data[offset + 3] << 24));
		offset += 4;
		return value;
	}
}
#nullable enable

public abstract class UnitySearchBase
{
	public abstract bool IsDirectory { get; }
	public AddressablesCatalog Catalog;
}

public class UnitySearchAsset : UnitySearchBase
{
	public override bool IsDirectory => false;

	public readonly object DependencyKey;
	public readonly string Name;
	public readonly CatalogResourceType Type;
	readonly object sync = new();

	public UnitySearchAsset(object dependencyKey, string name, CatalogResourceType type, AddressablesCatalog catalog) {
		DependencyKey = dependencyKey;
		Name = name;
		Type = type;
		Catalog = catalog;
	}

	private static bool BundleMatches(SerializedFile file, string bundleFile) =>
		Path.GetFileName((ReadOnlySpan<char>)file.originalPath).Equals(bundleFile, StringComparison.InvariantCulture);

	private static bool IsBundleLoaded(AssetsManager assets, string bundleFile) {
		foreach (var f in assets.AssetsFileList)
			if (BundleMatches(f, bundleFile))
				return true;
		return false;
	}

	public IEnumerable<SerializedFile> LoadNextBundle(string baseFolder, string platform, AssetsManager assets) {
		lock (sync) {
			var bundleFiles = Catalog.ResolveBundleFiles(DependencyKey);

			string? bundleFile = null;
			foreach (var candidate in bundleFiles) {
				if (!IsBundleLoaded(assets, candidate)) {
					bundleFile = candidate;
					break;
				}
			}
			if (bundleFile == null)
				yield break;

			HashSet<SerializedFile> previousState = assets.AssetsFileList.ToHashSet();
			assets.LoadFile(Path.Combine(baseFolder, platform, bundleFile));
			HashSet<SerializedFile> currentState = assets.AssetsFileList.ToHashSet();

			foreach (var bundle in currentState.Except(previousState)) {
				bundle.ReadAssets();
				yield return bundle;
			}
		}
	}

	public bool HasMoreBundles(AssetsManager assets) {
		var bundleFiles = Catalog.ResolveBundleFiles(DependencyKey);
		foreach (var candidate in bundleFiles)
			if (!IsBundleLoaded(assets, candidate))
				return true;
		return false;
	}

	public IEnumerable<SerializedFile> FindLoadedBundles(AssetsManager assets) {
		var bundleFiles = Catalog.ResolveBundleFiles(DependencyKey);
		foreach (var bundleFile in bundleFiles) {
			foreach (var f in assets.AssetsFileList) {
				if (BundleMatches(f, bundleFile)) {
					yield return f;
					break;
				}
			}
		}
	}
}

public class UnitySearchDirectory : UnitySearchBase
{
	public override bool IsDirectory => true;

	public readonly Dictionary<string, UnitySearchAsset> Files = new(StringComparer.Ordinal);
	public readonly Dictionary<string, UnitySearchDirectory> Directories = new(StringComparer.Ordinal);
	public readonly UnitySearchDirectory? Parent;
	public string FullyQualifiedPath {
		get => field ??= Parent == null ? null! : Parent.FullyQualifiedPath + Name + "/";
	}
	public ReadOnlySpan<char> PathHash => FullyQualifiedPath.AsSpan()[..^1];
	public readonly string? Name;

	public UnitySearchDirectory(string? name, AddressablesCatalog catalog) { Name = name; Catalog = catalog; }
	public UnitySearchDirectory(string name, AddressablesCatalog catalog, UnitySearchDirectory parent) { Name = name; Catalog = catalog; Parent = parent; }

	public UnitySearchDirectory? GetDirectory(ReadOnlySpan<char> dir) {
		if (!Directories.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
			return null;
		if (!lookup.TryGetValue(dir, out var ret))
			return null;
		return ret;
	}

	public UnitySearchDirectory? GetOrCreateDirectory(ReadOnlySpan<char> dir) {
		if (!Directories.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
			return null;

		if (!lookup.TryGetValue(dir, out UnitySearchDirectory? ret)) {
			ret = lookup[dir] = new(new(dir), Catalog, this);
			Catalog.PushUnitySearchBase(ret.PathHash, ret);
		}
		return ret;
	}

	public UnitySearchAsset? CreateFile(object primaryKey, object dependencyKey, CatalogResourceType type) {
		if (primaryKey is not string pk) return null;
		if (dependencyKey == null) return null;

		if (Files.TryGetValue(pk, out var ret))
			return null;

		ret = Files[pk] = new(dependencyKey, pk, type, Catalog);
		return ret;
	}
}

public enum UnitySearchResultStatus
{
	NotAFile = -100,
	NotFound = -99,
	UnknownError = 0,
	OK = 1
}

public struct UnitySearchResult<T>
{
	UnitySearchResultStatus Status;
	T? Result;

	public UnitySearchResult(UnitySearchResultStatus status) {
		Status = status;
	}

	public UnitySearchResult(T item) {
		Result = item;
		Status = UnitySearchResultStatus.OK;
	}

	public static UnitySearchResult<T> NotAFile() => new(UnitySearchResultStatus.NotAFile);
	public static UnitySearchResult<T> NotFound() => new(UnitySearchResultStatus.NotFound);

	public T GetRequiredResult() {
		if (Status != UnitySearchResultStatus.OK)
			throw new InvalidOperationException($"UnitySearchResult<T>: error {Status}");

		return Result!;
	}

	public T? GetResult() {
		return Result;
	}

	public bool IsOK() => Status == UnitySearchResultStatus.OK;
}

public class UnitySearchPathV2 : SearchPath
{
	public readonly AddressablesCatalog Catalog;
	public readonly AssetsManager Assets = new();
	public readonly Dictionary<string, SerializedFile> SerializedFiles = new();
	private readonly string _basePath;
	private readonly string _platform;

	readonly object sync = new();

	static string GetString(JsonElement o, string name)
	=> o.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String
		? e.GetString()! : null!;

	static byte[] ReadBase64(JsonElement o, string name)
		=> o.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String
			? e.GetBytesFromBase64() : Array.Empty<byte>();

	static string[] ReadStringArray(JsonElement o, string name) {
		if (!o.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
			return Array.Empty<string>();
		var result = new string[arr.GetArrayLength()];
		int i = 0;
		foreach (var el in arr.EnumerateArray())
			result[i++] = el.GetString()!;
		return result;
	}

	static CatalogProviderObjectType ReadObjectType(JsonElement e) {
		if (e.ValueKind != JsonValueKind.Object) return null!;

		return new CatalogProviderObjectType {
			m_AssemblyName = GetString(e, "m_AssemblyName"),
			m_ClassName = GetString(e, "m_ClassName"),
		};
	}

	static CatalogProviderData ReadProviderDataElement(JsonElement e) {
		if (e.ValueKind != JsonValueKind.Object) return null!;
		return new CatalogProviderData {
			m_Id = GetString(e, "m_Id"),
			m_ObjectType = e.TryGetProperty("m_ObjectType", out var ot) ? ReadObjectType(ot) : null,
			m_Data = GetString(e, "m_Data"),
		};
	}

	static CatalogProviderData ReadProviderData(JsonElement o, string name)
		=> o.TryGetProperty(name, out var e) ? ReadProviderDataElement(e) : null!;

	static CatalogProviderData[] ReadProviderDataArray(JsonElement o, string name) {
		if (!o.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
			return Array.Empty<CatalogProviderData>();
		var result = new CatalogProviderData[arr.GetArrayLength()];
		int i = 0;
		foreach (var el in arr.EnumerateArray())
			result[i++] = ReadProviderDataElement(el);
		return result;
	}

	static CatalogResourceType[] ReadResourceTypes(JsonElement o, string name) {
		if (!o.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
			return Array.Empty<CatalogResourceType>();
		var result = new CatalogResourceType[arr.GetArrayLength()];
		int i = 0;
		foreach (var el in arr.EnumerateArray()) {
			string m_ClassName = GetString(el, "m_ClassName");
			result[i++] = new CatalogResourceType {
				m_AssemblyName = GetString(el, "m_AssemblyName"),
				m_ClassName = m_ClassName,
				IsAssetBundleResource = m_ClassName == "UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource"
			};
		}
		return result;
	}

	public UnitySearchPathV2(string whereIsStreamingAssetsAA, string standalonePlatform) {
		_basePath = whereIsStreamingAssetsAA;
		_platform = standalonePlatform;

		string catalogJsonPath = Path.Combine(whereIsStreamingAssetsAA, "catalog.json");

		Catalog = new();
		using (Stream jsonStream = File.OpenRead(catalogJsonPath))
		using (JsonDocument doc = JsonDocument.Parse(jsonStream)) {
			JsonElement root = doc.RootElement;

			Catalog.m_LocatorId = GetString(root, "m_LocatorId");
			Catalog.m_BuildResultHash = GetString(root, "m_BuildResultHash");

			Catalog.m_InstanceProviderData = ReadProviderData(root, "m_InstanceProviderData");
			Catalog.m_SceneProviderData = ReadProviderData(root, "m_SceneProviderData");
			Catalog.m_ResourceProviderData = ReadProviderDataArray(root, "m_ResourceProviderData");

			Catalog.m_ProviderIds = ReadStringArray(root, "m_ProviderIds");
			Catalog.m_InternalIds = ReadStringArray(root, "m_InternalIds");
			Catalog.m_resourceTypes = ReadResourceTypes(root, "m_resourceTypes");

			Parallel.Invoke(
				() => Catalog.m_KeyData = ReadBase64(root, "m_KeyDataString"),
				() => Catalog.m_BucketData = ReadBase64(root, "m_BucketDataString"),
				() => Catalog.m_EntryData = ReadBase64(root, "m_EntryDataString"),
				() => Catalog.m_ExtraData = ReadBase64(root, "m_ExtraDataString")
			);
		}
		Catalog.Decode();
	}

	public record struct CachedObjectLookup_t(SerializedFile File, long PathID)
	{
		public AssetStudio.Object Get() => File.ObjectsDic[PathID];
	}

	public readonly Dictionary<string, CachedObjectLookup_t> CachedObjectFullyQualifiedLookup = new(StringComparer.Ordinal);
	public readonly Dictionary<string, List<CachedObjectLookup_t>> CachedObjectFileNameLookup = new(StringComparer.Ordinal);
	public readonly Dictionary<long, AssetStudio.Object> CachedObjectPathIDLookup = [];

	public long MaxResidentBytes = 128L * 1024 * 1024;

	private long _accessCounter;
	private long _residentBytes;
	private readonly Dictionary<SerializedFile, long> _bundleAccess = [];
	private readonly Dictionary<SerializedFile, long> _bundleSize = [];

	private static long BundleSize(SerializedFile file) {
		try { return file.reader?.BaseStream?.Length ?? 0; }
		catch { return 0; }
	}

	private void Touch(SerializedFile file) {
		_bundleAccess[file] = ++_accessCounter;
		if (!_bundleSize.ContainsKey(file)) {
			long size = BundleSize(file);
			_bundleSize[file] = size;
			_residentBytes += size;
		}
	}

	private void EvictToLimit() {
		while (_residentBytes > MaxResidentBytes && _bundleAccess.Count > 1) {
			SerializedFile? lru = null;
			long oldest = long.MaxValue;
			foreach (var kvp in _bundleAccess) {
				if (kvp.Value < oldest) {
					oldest = kvp.Value;
					lru = kvp.Key;
				}
			}
			if (lru == null)
				break;
			EvictBundle(lru);
		}
	}

	private void EvictBundle(SerializedFile bundle) {
		foreach (var obj in bundle.Objects) {
			if (CachedObjectPathIDLookup.TryGetValue(obj.m_PathID, out var cur) && cur == obj)
				CachedObjectPathIDLookup.Remove(obj.m_PathID);
		}

		List<string>? dropFq = null;
		foreach (var kvp in CachedObjectFullyQualifiedLookup) {
			if (kvp.Value.File == bundle)
				(dropFq ??= []).Add(kvp.Key);
		}
		if (dropFq != null)
			foreach (var k in dropFq)
				CachedObjectFullyQualifiedLookup.Remove(k);

		List<string>? emptied = null;
		foreach (var kvp in CachedObjectFileNameLookup) {
			kvp.Value.RemoveAll(l => l.File == bundle);
			if (kvp.Value.Count == 0)
				(emptied ??= []).Add(kvp.Key);
		}
		if (emptied != null)
			foreach (var k in emptied)
				CachedObjectFileNameLookup.Remove(k);

		SerializedFiles.Remove(bundle.fileName);
		_bundleAccess.Remove(bundle);
		if (_bundleSize.TryGetValue(bundle, out var sz)) {
			_residentBytes -= sz;
			_bundleSize.Remove(bundle);
		}
		Assets.UnloadFile(bundle);
	}

	public void UnloadAll() {
		lock (sync) {
			CachedObjectFullyQualifiedLookup.Clear();
			CachedObjectFileNameLookup.Clear();
			CachedObjectPathIDLookup.Clear();
			SerializedFiles.Clear();
			_bundleAccess.Clear();
			_bundleSize.Clear();
			_residentBytes = 0;
			Assets.Clear();
		}
	}

	private void CacheNewBundles(IEnumerable<SerializedFile> newBundles) {
		foreach (var bundleLoaded in newBundles) {
			SerializedFiles[bundleLoaded.fileName] = bundleLoaded;
			Touch(bundleLoaded);
			foreach (var obj in bundleLoaded.Objects) {
				CachedObjectPathIDLookup[obj.m_PathID] = obj;
				string? name = obj.GetUnityName();
				if (name != null) {
					string h = name;
					if (!CachedObjectFileNameLookup.TryGetValue(h, out var list))
						CachedObjectFileNameLookup[h] = list = [];
					list.Add(new() { File = bundleLoaded, PathID = obj.m_PathID });
				}
			}
		}
		EvictToLimit();
	}

	private static T? FindTypedInCache<T>(List<CachedObjectLookup_t> lookups) where T : AssetStudio.Object {
		foreach (var l in lookups) {
			if (l.Get() is T typed)
				return typed;
		}
		return null;
	}

	private static AssetStudio.Object? FindInBundle<T>(SerializedFile bundle, ReadOnlySpan<char> name) where T : AssetStudio.Object {
		for (int i = 0, c = bundle.Objects.Count; i < c; i++) {
			AssetStudio.Object? o = bundle.Objects[i];
			if (o != null && o is T && o.GetUnityName() == name)
				return o;
		}
		return null;
	}

	private T? SearchAssetInBundles<T>(UnitySearchAsset searchAsset, ReadOnlySpan<char> name) where T : AssetStudio.Object {
		if (!CachedObjectFileNameLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var fnLookup))
			throw new Exception();

		foreach (var bundle in searchAsset.FindLoadedBundles(Assets)) {
			var match = FindInBundle<T>(bundle, name);
			if (match != null) {
				// Cache it
				if (!fnLookup.TryGetValue(name, out var cacheList))
					fnLookup[name] = cacheList = [];
				cacheList.Add(new() { File = bundle, PathID = match.m_PathID });
				Touch(bundle);
				return (T)match;
			}
		}

		while (searchAsset.HasMoreBundles(Assets)) {
			var newBundles = searchAsset.LoadNextBundle(_basePath, _platform, Assets);
			CacheNewBundles(newBundles);

			if (fnLookup.TryGetValue(name, out var lookups)) {
				var cached = FindTypedInCache<T>(lookups);
				if (cached != null)
					return cached;
			}

			foreach (var bundle in searchAsset.FindLoadedBundles(Assets)) {
				var match = FindInBundle<T>(bundle, name);
				if (match != null) {
					if (!fnLookup.TryGetValue(name, out var cacheList))
						fnLookup[name] = cacheList = [];
					cacheList.Add(new() { File = bundle, PathID = match.m_PathID });
					return (T)match;
				}
			}
		}

		return null;
	}

	public UnitySearchResult<T> LoadAsset<T>(ReadOnlySpan<char> path) where T : AssetStudio.Object {
		lock (sync) {
			var asset = Catalog.Search(path);
			if (asset == null)
				return UnitySearchResult<T>.NotFound();

			if (asset is UnitySearchAsset searchAsset) {
				if (!CachedObjectFullyQualifiedLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var altFql))
					throw new Exception();
				if (!CachedObjectFileNameLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var altFiles))
					throw new Exception();

				if (altFql.TryGetValue(path, out var lookup)) {
					if (lookup.Get() is T typed) {
						Touch(lookup.File);
						return new(typed);
					}
				}

				ReadOnlySpan<char> pathFileName = Path.GetFileName((ReadOnlySpan<char>)path);

				if (altFiles.TryGetValue(pathFileName, out var lookups)) {
					var cached = FindTypedInCache<T>(lookups);
					if (cached != null) {
						var cachedLookup = lookups.First(l => l.Get() is T);
						altFql[path] = cachedLookup;
						Touch(cachedLookup.File);
						return new(cached);
					}
				}

				var result = SearchAssetInBundles<T>(searchAsset, pathFileName);
				if (result == null)
					return UnitySearchResult<T>.NotFound();

				if (altFiles.TryGetValue(pathFileName, out var fnLookups)) {
					var matchingLookup = fnLookups.FirstOrDefault(l => l.Get() is T);
					altFql[path] = matchingLookup;
				}

				return new(result);
			}
			else if (asset is UnitySearchDirectory dir) {
				ReadOnlySpan<char> filenameNoExt = Path.GetFileNameWithoutExtension(dir.FullyQualifiedPath.AsSpan()[..^1]);
				Span<char> mergePath = stackalloc char[dir.FullyQualifiedPath.Length + filenameNoExt.Length];
				dir.FullyQualifiedPath.CopyTo(mergePath);
				filenameNoExt.CopyTo(mergePath[dir.FullyQualifiedPath.Length..]);

				foreach (var file in dir.Files) {
					UnitySearchResult<T> result = LoadAsset<T>(mergePath);
					if (result.IsOK())
						return result;
				}

				return UnitySearchResult<T>.NotAFile();
			}
			else throw new NotImplementedException();
		}
	}

	public T? FindAssetByName<T>(ReadOnlySpan<char> name) where T : AssetStudio.Object {
		if (!CachedObjectFileNameLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var fnLookup))
			throw new Exception();

		lock (sync) {
			if (fnLookup.TryGetValue(name, out var lookups)) {
				var cached = FindTypedInCache<T>(lookups);
				if (cached != null) {
					Touch(lookups.First(l => l.Get() is T).File);
					return cached;
				}
			}

			var allMatches = Catalog.SearchAll(name);
			if (allMatches.Count == 0)
				return null;

			foreach (var entry in allMatches) {
				if (entry is not UnitySearchAsset searchAsset)
					continue;

				var result = SearchAssetInBundles<T>(searchAsset, name);
				if (result != null)
					return result;
			}

			return null;
		}
	}

	public T? FindAssetByPathID<T>(long pathID) where T : AssetStudio.Object {
		lock (sync)
			return (T?)(CachedObjectPathIDLookup.TryGetValue(pathID, out var obj) ? obj : null);
	}

	public override bool CheckDirectory(ReadOnlySpan<char> path, FileAccess? specificAccess = null, FileMode? specificMode = null) => Catalog.Search(path)?.IsDirectory ?? false;
	public override bool CheckFile(ReadOnlySpan<char> path, FileAccess? specificAccess, FileMode? specificMode) => Catalog.Search(path) != null;

	public override IEnumerable<string> FindDirectories(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}

	public override IEnumerable<string> FindFiles(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}

	protected override Stream? OnOpen(ReadOnlySpan<char> path, FileAccess access, FileMode open) {
		lock (sync) {
			AssetStudio.Object? asset = LoadAsset<AssetStudio.Object>(path).GetResult();
			switch (asset) {
				case TextAsset ta: return new MemoryStream(ta.m_Script);
				default: throw new NotImplementedException($"No way to explicitly pull a stream out of a {asset.GetType().FullName}.");
			}
		}
	}
}
