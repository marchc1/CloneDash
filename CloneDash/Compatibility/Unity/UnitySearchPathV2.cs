using AssetStudio;
using Newtonsoft.Json;
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
}

/// <summary>
/// A parsed catalog entry location
/// </summary>
public class CatalogEntryLocation
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
	public string m_KeyDataString;
	public string m_BucketDataString;
	public string m_EntryDataString;
	public string m_ExtraDataString;

	[JsonIgnore] public byte[] m_KeyData;
	[JsonIgnore] public byte[] m_BucketData;
	[JsonIgnore] public byte[] m_EntryData;
	[JsonIgnore] public byte[] m_ExtraData;

	public CatalogResourceType[] m_resourceTypes;

	[JsonIgnore] public List<object> Keys { get; private set; }
	[JsonIgnore] public Dictionary<object, List<CatalogEntryLocation>> Locations { get; private set; }

	[JsonIgnore] public readonly Dictionary<string, int> InternalIDReferences = [];
	public int AddInternalIDRef(string name) {
		if (!InternalIDReferences.TryGetValue(name, out int v))
			v = 0;
		InternalIDReferences[name] = v++;
		return v;
	}
	public bool DoesInternalIDContainMultipleNames(string name) => InternalIDReferences.TryGetValue(name, out int v) && v > 1;

	[JsonIgnore] public List<CatalogEntryLocation> AllEntries { get; private set; }
	[JsonIgnore] bool Decoded;
	[JsonIgnore] public readonly Dictionary<ulong, List<UnitySearchBase>> HashedAssetLookup = [];

	public void PushUnitySearchBase(ulong hashedKey, UnitySearchBase searchBase) {
		if (!HashedAssetLookup.TryGetValue(hashedKey, out var list))
			list = HashedAssetLookup[hashedKey] = [];
		list.Add(searchBase);
	}


#nullable enable
	public UnitySearchBase? Search(ReadOnlySpan<char> forWhat) {
		ulong hash = forWhat.SliceNullTerminatedString().Hash();
		if (!HashedAssetLookup.TryGetValue(hash, out var list))
			return null;
		return list.FirstOrDefault();
	}
	public IReadOnlyList<UnitySearchBase> SearchAll(ReadOnlySpan<char> forWhat) {
		ulong hash = forWhat.SliceNullTerminatedString().Hash();
		if (!HashedAssetLookup.TryGetValue(hash, out var list))
			return [];
		return list;
	}
	public UnitySearchBase? Search<T>(ReadOnlySpan<char> forWhat) {
		ulong hash = forWhat.SliceNullTerminatedString().Hash();
		if (!HashedAssetLookup.TryGetValue(hash, out var list))
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

	readonly Dictionary<object, List<string>> DependencyKeyToBundleFiles = [];

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

	private static string? ExtractBundleFileName(string internalId) {
		if (internalId == null)
			return null;

		// Handle the {UnityEngine.AddressableAssets.Addressables.RuntimePath}/Platform/bundle.bundle format
		int prefixEnd = internalId.IndexOf('}');
		if (prefixEnd >= 0) {
			// Skip past the closing brace and the following slash
			ReadOnlySpan<char> afterPrefix = internalId.AsSpan()[(prefixEnd + 1)..];
			return Path.GetFileName(afterPrefix).ToString();
		}

		// just get the filename
		return Path.GetFileName(internalId);
	}

	public void Decode() {
		if (Decoded)
			return;

		m_KeyData = Convert.FromBase64String(m_KeyDataString);
		m_BucketData = Convert.FromBase64String(m_BucketDataString);
		m_EntryData = Convert.FromBase64String(m_EntryDataString);
		m_ExtraData = Convert.FromBase64String(m_ExtraDataString);

		Keys = ReadKeys();
		AllEntries = ReadEntries();
		Locations = ReadBuckets();
		foreach (var entry in AllEntries)
			AddInternalIDRef(entry.InternalId);

		foreach (var kvp in Locations) {
			foreach (var loc in kvp.Value) {
				if (loc.ResourceType?.m_ClassName != "UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource")
					continue;

				string bundleFileName = ExtractBundleFileName(loc.InternalId);
				if (bundleFileName != null) {
					if (!DependencyKeyToBundleFiles.TryGetValue(kvp.Key, out var list))
						DependencyKeyToBundleFiles[kvp.Key] = list = [];
					if (!list.Contains(bundleFileName))
						list.Add(bundleFileName);
				}
			}
		}

		for (int i = 0, c = AllEntries.Count; i < c; i++) {
			var entry = AllEntries[i];
			if (entry.ResourceType?.m_ClassName != "UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource")
				continue;
			if (entry.PrimaryKey == null)
				continue;

			string bundleFileName = ExtractBundleFileName(entry.InternalId);
			if (bundleFileName != null) {
				if (!DependencyKeyToBundleFiles.TryGetValue(entry.PrimaryKey, out var list))
					DependencyKeyToBundleFiles[entry.PrimaryKey] = list = [];
				if (!list.Contains(bundleFileName))
					list.Add(bundleFileName);
			}
		}

		for (int i = 0, c = AllEntries.Count; i < c; i++) {
			var entry = AllEntries[i];
			if (entry.ResourceType?.m_ClassName == "UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource")
				continue;

			if (entry.InternalId == null || entry.DependencyKey == null)
				continue;

			if (!DependencyKeyToBundleFiles.ContainsKey(entry.DependencyKey))
				continue;

			ContainerParser parser = new(entry.InternalId);
			UnitySearchDirectory dir = Root;
			while (parser.TryPiece(out ReadOnlySpan<char> piece, out bool last)) {
				dir = dir.GetOrCreateDirectory(piece);
				ulong hash = dir.FullyQualifiedPath.AsSpan()[..^1].Hash();
				if (!HashedAssetLookup.TryGetValue(hash, out _))
					PushUnitySearchBase(hash, dir);
			}

			UnitySearchAsset asset = dir.CreateFile(entry.PrimaryKey, entry.DependencyKey, entry.ResourceType);
			if (asset != null) {
				string fullName = dir.FullyQualifiedPath + asset.Name;
				PushUnitySearchBase(fullName.Hash(), asset);          // Container/Name

				PushUnitySearchBase(asset.Name.Hash(), asset);        // PrimaryKey

				// Also index by just the filename portion of the PrimaryKey,
				// so callers using short names (e.g. "s01_arrow") can still find
				// assets whose address includes a folder prefix (e.g. "AtlasH/s01_arrow")
				// TODO: Review if we should keep this behavior? It's what AssetStudioGUI reports...
				// we'll see.
				int lastSlash = asset.Name.LastIndexOf('/');
				if (lastSlash >= 0) {
					string shortName = asset.Name[(lastSlash + 1)..];
					PushUnitySearchBase(shortName.Hash(), asset);
				}
			}
		}
		Decoded = true;

		m_KeyData = null;
		m_BucketData = null;
		m_EntryData = null;
		m_ExtraData = null;
		Keys = null;
		AllEntries = null;
		Locations = null;
	}

	private List<object> ReadKeys() {
		var keys = new List<object>();
		int offset = 0;

		int keyCount = ReadInt32(m_KeyData, ref offset);

		for (int i = 0; i < keyCount; i++) {
			keys.Add(ReadKey(m_KeyData, ref offset));
		}

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

	private List<CatalogEntryLocation> ReadEntries() {
		var entries = new List<CatalogEntryLocation>();
		int offset = 0;

		int entryCount = ReadInt32(m_EntryData, ref offset);

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
				DependencyKey = (depKeyIdx >= 0 && depKeyIdx < Keys.Count)
					? Keys[depKeyIdx]
					: null,
				ResourceType = (resourceTypeIdx >= 0 && m_resourceTypes != null && resourceTypeIdx < m_resourceTypes.Length)
					? m_resourceTypes[resourceTypeIdx]
					: null,
				PrimaryKey = (primaryKeyIdx >= 0 && primaryKeyIdx < Keys.Count)
					? Keys[primaryKeyIdx]
					: null,
				EntryIndex = i,
				DataIndex = dataIdx
			};

			entries.Add(entry);
		}

		return entries;
	}

	private Dictionary<object, List<CatalogEntryLocation>> ReadBuckets() {
		var locations = new Dictionary<object, List<CatalogEntryLocation>>();
		int offset = 0;

		int bucketCount = ReadInt32(m_BucketData, ref offset);

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

			var entryList = new List<CatalogEntryLocation>();
			foreach (int idx in entryIndices) {
				if (idx >= 0 && idx < AllEntries.Count)
					entryList.Add(AllEntries[idx]);
			}

			if (!locations.ContainsKey(key))
				locations[key] = entryList;
			else
				locations[key].AddRange(entryList);
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
	public readonly string Container;
	public readonly string Name;
	public readonly CatalogResourceType Type;
	readonly object sync = new();

	public UnitySearchAsset(object dependencyKey, string container, string name, CatalogResourceType type, AddressablesCatalog catalog) {
		DependencyKey = dependencyKey;
		Container = container;
		Name = name;
		Type = type;
		Catalog = catalog;
	}

	public int BundlesLoadedSoFar;

	public IEnumerable<SerializedFile> LoadNextBundle(string baseFolder, string platform, AssetsManager assets) {
		lock (sync) {
			var bundleFiles = Catalog.ResolveBundleFiles(DependencyKey);
			if (BundlesLoadedSoFar >= bundleFiles.Count)
				yield break;

			string bundleFile = bundleFiles[BundlesLoadedSoFar];
			BundlesLoadedSoFar++;

			// Skip if already loaded
			if (assets.assetsFileList.Any(x => Path.GetFileName((ReadOnlySpan<char>)x.originalPath).Equals(bundleFile, StringComparison.InvariantCulture)))
				yield break;

			HashSet<SerializedFile> previousState = assets.assetsFileList.ToHashSet();
			assets.LoadFile(Path.Combine(baseFolder, platform, bundleFile));
			HashSet<SerializedFile> currentState = assets.assetsFileList.ToHashSet();

			foreach (var bundle in currentState.Except(previousState)) {
				bundle.ReadAssets();
				yield return bundle;
			}
		}
	}

	public bool HasMoreBundles => BundlesLoadedSoFar < Catalog.ResolveBundleFiles(DependencyKey).Count;

	public IEnumerable<SerializedFile> FindLoadedBundles(AssetsManager assets) {
		var bundleFiles = Catalog.ResolveBundleFiles(DependencyKey);
		int limit = Math.Min(BundlesLoadedSoFar, bundleFiles.Count);
		for (int i = 0; i < limit; i++) {
			string bundleFile = bundleFiles[i];
			var match = assets.assetsFileList.FirstOrDefault(x =>
				Path.GetFileName((ReadOnlySpan<char>)x.originalPath).Equals(bundleFile, StringComparison.InvariantCulture));
			if (match != null)
				yield return match;
		}
	}
}

public class UnitySearchDirectory : UnitySearchBase
{
	public override bool IsDirectory => true;

	public readonly Dictionary<UtlSymbol, UnitySearchAsset> Files = [];
	public readonly Dictionary<UtlSymbol, UnitySearchDirectory> Directories = [];
	public readonly UnitySearchDirectory? Parent;
	public readonly string FullyQualifiedPath = "";
	public readonly string? Name;

	public UnitySearchDirectory(string? name, AddressablesCatalog catalog) { Name = name; Catalog = catalog; }
	public UnitySearchDirectory(string name, AddressablesCatalog catalog, UnitySearchDirectory parent) { Name = name; Catalog = catalog; Parent = parent; FullyQualifiedPath = parent.FullyQualifiedPath + Name + "/"; }

	public UnitySearchDirectory? GetDirectory(ReadOnlySpan<char> dir) {
		UtlSymbol symbol = new(dir);
		if (!Directories.TryGetValue(symbol, out var ret))
			return null;
		return ret;
	}

	public UnitySearchDirectory? GetOrCreateDirectory(ReadOnlySpan<char> dir) {
		UtlSymbol symbol = new(dir);
		if (!Directories.TryGetValue(symbol, out var ret))
			ret = Directories[symbol] = new(new(dir), Catalog, this);
		return ret;
	}

	public UnitySearchAsset? CreateFile(object primaryKey, object dependencyKey, CatalogResourceType type) {
		if (primaryKey is not string pk) return null;
		if (dependencyKey == null) return null;

		UtlSymbol symbol = new(pk);
		if (Files.TryGetValue(symbol, out var ret))
			return null;

		ret = Files[symbol] = new(dependencyKey, FullyQualifiedPath, pk, type, Catalog);
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
	public readonly Dictionary<ulong, SerializedFile> SerializedFiles = new();
	private readonly string _basePath;
	private readonly string _platform;

	readonly object sync = new();

	public UnitySearchPathV2(string whereIsStreamingAssetsAA, string standalonePlatform) {
		_basePath = whereIsStreamingAssetsAA;
		_platform = standalonePlatform;

		string catalogJsonPath = Path.Combine(whereIsStreamingAssetsAA, "catalog.json");
		Catalog = JsonConvert.DeserializeObject<AddressablesCatalog>(File.ReadAllText(catalogJsonPath))!;
		Catalog.Decode();
	}

	public record struct CachedObjectLookup_t(SerializedFile File, long PathID)
	{
		public AssetStudio.Object Get() => File.ObjectsDic[PathID];
	}

	public readonly Dictionary<ulong, CachedObjectLookup_t> CachedObjectFullyQualifiedLookup = [];
	public readonly Dictionary<ulong, List<CachedObjectLookup_t>> CachedObjectFileNameLookup = [];
	public readonly Dictionary<long, AssetStudio.Object> CachedObjectPathIDLookup = [];

	private void CacheNewBundles(IEnumerable<SerializedFile> newBundles) {
		foreach (var bundleLoaded in newBundles) {
			SerializedFiles[bundleLoaded.fileName.Hash()] = bundleLoaded;
			foreach (var obj in bundleLoaded.Objects) {
				CachedObjectPathIDLookup[obj.m_PathID] = obj;
				string? name = obj.GetUnityName();
				if (name != null) {
					ulong h = name.Hash();
					if (!CachedObjectFileNameLookup.TryGetValue(h, out var list))
						CachedObjectFileNameLookup[h] = list = [];
					list.Add(new() { File = bundleLoaded, PathID = obj.m_PathID });
				}
			}
		}
	}

	private static T? FindTypedInCache<T>(List<CachedObjectLookup_t> lookups) where T : AssetStudio.Object {
		foreach (var l in lookups) {
			if (l.Get() is T typed)
				return typed;
		}
		return null;
	}

	private static AssetStudio.Object? FindInBundle<T>(SerializedFile bundle, ulong nameHash) where T : AssetStudio.Object {
		return bundle.Objects.FirstOrDefault(x => x is T && (x.GetUnityName()?.Hash() ?? 0) == nameHash);
	}

	private T? SearchAssetInBundles<T>(UnitySearchAsset searchAsset, ulong nameHash) where T : AssetStudio.Object {
		foreach (var bundle in searchAsset.FindLoadedBundles(Assets)) {
			var match = FindInBundle<T>(bundle, nameHash);
			if (match != null) {
				// Cache it
				if (!CachedObjectFileNameLookup.TryGetValue(nameHash, out var cacheList))
					CachedObjectFileNameLookup[nameHash] = cacheList = [];
				cacheList.Add(new() { File = bundle, PathID = match.m_PathID });
				return (T)match;
			}
		}

		while (searchAsset.HasMoreBundles) {
			var newBundles = searchAsset.LoadNextBundle(_basePath, _platform, Assets);
			CacheNewBundles(newBundles);

			if (CachedObjectFileNameLookup.TryGetValue(nameHash, out var lookups)) {
				var cached = FindTypedInCache<T>(lookups);
				if (cached != null)
					return cached;
			}

			foreach (var bundle in searchAsset.FindLoadedBundles(Assets)) {
				var match = FindInBundle<T>(bundle, nameHash);
				if (match != null) {
					if (!CachedObjectFileNameLookup.TryGetValue(nameHash, out var cacheList))
						CachedObjectFileNameLookup[nameHash] = cacheList = [];
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
				ulong hash = path.Hash();

				if (CachedObjectFullyQualifiedLookup.TryGetValue(hash, out var lookup)) {
					if (lookup.Get() is T typed)
						return new(typed);
				}

				ReadOnlySpan<char> pathFileName = Path.GetFileName((ReadOnlySpan<char>)path);
				ulong pathFileNameHash = pathFileName.Hash();

				if (CachedObjectFileNameLookup.TryGetValue(pathFileNameHash, out var lookups)) {
					var cached = FindTypedInCache<T>(lookups);
					if (cached != null) {
						var cachedLookup = lookups.First(l => l.Get() is T);
						CachedObjectFullyQualifiedLookup[hash] = cachedLookup;
						return new(cached);
					}
				}

				var result = SearchAssetInBundles<T>(searchAsset, pathFileNameHash);
				if (result == null)
					return UnitySearchResult<T>.NotFound();

				if (CachedObjectFileNameLookup.TryGetValue(pathFileNameHash, out var fnLookups)) {
					var matchingLookup = fnLookups.FirstOrDefault(l => l.Get() is T);
					CachedObjectFullyQualifiedLookup[hash] = matchingLookup;
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

	public T? FindAssetByName<T>(string name) where T : AssetStudio.Object {
		lock (sync) {
			ulong nameHash = name.Hash();

			if (CachedObjectFileNameLookup.TryGetValue(nameHash, out var lookups)) {
				var cached = FindTypedInCache<T>(lookups);
				if (cached != null)
					return cached;
			}

			var allMatches = Catalog.SearchAll(name);
			if (allMatches.Count == 0)
				return null;

			foreach (var entry in allMatches) {
				if (entry is not UnitySearchAsset searchAsset)
					continue;

				var result = SearchAssetInBundles<T>(searchAsset, nameHash);
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
