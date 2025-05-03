using AssetStudio;
using Newtonsoft.Json;
using Nucleus;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CloneDash.Systems;

public class RawUnityAssetCatalog
{
	[JsonProperty("m_ProviderIds")] public string[] ProviderIds;
	[JsonProperty("m_InternalIds")] public string[] InternalIds;
	[JsonProperty("m_KeyDataString")] public string KeyDataString;
	[JsonProperty("m_BucketDataString")] public string BucketDataString;
	[JsonProperty("m_EntryDataString")] public string EntryDataString;
	[JsonProperty("m_ExtraDataString")] public string ExtraDataString;

	private byte[]? keydata, bucketdata, entrydata, extradata;

	public byte[] KeyData {
		get {
			if (keydata == null)
				keydata = Convert.FromBase64String(KeyDataString);
			return keydata;
		}
	}

	public byte[] BucketData {
		get {
			if (bucketdata == null)
				bucketdata = Convert.FromBase64String(BucketDataString);
			return bucketdata;
		}
	}

	public byte[] EntryData {
		get {
			if (entrydata == null)
				entrydata = Convert.FromBase64String(EntryDataString);
			return entrydata;
		}
	}

	public byte[] ExtraData {
		get {
			if (extradata == null)
				extradata = Convert.FromBase64String(ExtraDataString);
			return extradata;
		}
	}
}

public class UnityKey
{
	public byte UnknownByte;
	public string Key;

	public static implicit operator string(UnityKey self) => self.Key;

	public override string ToString() {
		return $"Unity Key '{Key}' [{UnknownByte}]";
	}
}

public class UnityBucket
{
	public int DataOffset;
	public int[] Entries;

	public override string ToString() {
		return $"Unity Bucket ({Entries.Length} entries, offset +{DataOffset})";
	}
}

public class UnityEntry
{
	public string InternalID;
	public string ProviderID;
	public UnityKey? Dependency;
	public int DependencyHash;

	public int DataOffset;
	public UnityKey[] Keys;

	public int ResourceTypeIndex;
}

public class UnityBundleSearcher
{
	UnityAssetCatalog catalog;
	Dictionary<string, string> localKeyNameToBundleName = [];
	Dictionary<string, string> assetPathToBundleName = [];
	Dictionary<string, UnityEntry> assetPathToEntry = [];
	Dictionary<string, HashSet<UnityEntry>> bundleNameToEntries = [];

	public UnityBundleSearcher(UnityAssetCatalog catalog) {
		this.catalog = catalog;

		assetPathToBundleName.EnsureCapacity(catalog.Entries.Length);
		bundleNameToEntries.EnsureCapacity(catalog.Entries.Length);

		foreach (var entry in this.catalog.Entries) {
			var internalID = entry.InternalID;
			if (internalID.StartsWith("{UnityEngine.AddressableAssets.Addressables.RuntimePath}")) {
#if COMPILED_WINDOWS
				internalID = internalID.Replace($"{{UnityEngine.AddressableAssets.Addressables.RuntimePath}}\\{MuseDashCompatibility.StandalonePlatform}\\", "");
#else
				internalID = internalID.Replace($"{{UnityEngine.AddressableAssets.Addressables.RuntimePath}}/{MuseDashCompatibility.StandalonePlatform}/", "");
#endif
				localKeyNameToBundleName[entry.Keys[0].Key] = internalID;
				bundleNameToEntries[internalID] = [];
			}
			else if (entry.Dependency != null) {
				var dep = entry.Dependency;

				if (localKeyNameToBundleName.TryGetValue(dep.Key, out string? bundleName) && bundleNameToEntries.TryGetValue(bundleName, out var entries)) {
					entries.Add(entry);
					assetPathToBundleName[entry.InternalID] = bundleName;
					assetPathToEntry[entry.InternalID] = entry;
				}
			}
		}
	}

	public IEnumerable<UnityEntry> Entries(string localBundleName) {
		localKeyNameToBundleName.TryGetValue(localBundleName, out string? bundleName);
		bundleNameToEntries.TryGetValue(bundleName, out var entries);

		foreach (var entry in entries)
			yield return entry;
	}

	public string Search(string name) => Path.Combine(MuseDashCompatibility.BuildTarget, assetPathToBundleName[name]);
}

public class UnityAssetCatalog
{
	private static string ReadStringUTF8(BinaryReader reader) {
		int len = reader.ReadInt32();
		byte[] stringBytes = reader.ReadBytes(len);
		string str = Encoding.UTF8.GetString(stringBytes);
		return str;
	}

	private RawUnityAssetCatalog RawData;

	public UnityKey[] Keys;
	public UnityBucket[] Buckets;
	public UnityEntry[] Entries;

	private Dictionary<string, UnityEntry> entryLookup = [];

	public UnityEntry? Lookup(string name) => entryLookup.TryGetValue(name, out UnityEntry? entry) ? entry : null;

	public UnityAssetCatalog(string catalogFilepath) {
		using (StreamReader reader = new(File.OpenRead(catalogFilepath), leaveOpen: false)) {
			RawData = JsonConvert.DeserializeObject<RawUnityAssetCatalog>(reader.ReadToEnd()) ?? throw new Exception();

			using (MemoryStream ms = new(RawData.KeyData))
			using (BinaryReader br = new(ms)) {
				var keys = br.ReadInt32();
				Keys = new UnityKey[keys];
				for (int i = 0; i < keys; i++) {
					br.ReadByte();
					Keys[i] = new() { Key = ReadStringUTF8(br) };
				}
			}

			using (MemoryStream ms = new(RawData.BucketData))
			using (BinaryReader br = new(ms)) {
				var buckets = br.ReadInt32();
				Buckets = new UnityBucket[buckets];
				for (int i = 0; i < buckets; i++) {
					int dataOffset = br.ReadInt32();
					int len = br.ReadInt32();
					int[] indices = new int[len];
					for (int j = 0; j < len; j++)
						indices[j] = br.ReadInt32();

					Buckets[i] = new() { DataOffset = dataOffset, Entries = indices };
				}
			}

			using (MemoryStream ms = new(RawData.EntryData, 0, RawData.EntryData.Length, false, publiclyVisible: true))
			using (BinaryReader br = new(ms)) {
				var entries = br.ReadInt32();
				Entries = new UnityEntry[entries];
				for (int i = 0; i < entries; i++) { // 28 bytes per entry?
					int internalIndex = br.ReadInt32();
					int providerIndex = br.ReadInt32();
					int depKeyIndex = br.ReadInt32();
					int depHash = br.ReadInt32();
					int dataOffset = br.ReadInt32();
					int bucketIndex = br.ReadInt32();
					int resTypeIndex = br.ReadInt32();

					string internalID = RawData.InternalIds[internalIndex];
					string providerID = RawData.ProviderIds[providerIndex];

					var bucket = Buckets[bucketIndex];
					UnityKey[] keys = new UnityKey[bucket.Entries.Length];
					for (int k = 0, n = bucket.Entries.Length; k < n; k++)
						keys[k] = Keys[bucket.Entries[k]];

					Entries[i] = new() {
						InternalID = internalID,
						ProviderID = providerID,
						Dependency = depKeyIndex < 0 ? null : Keys[depKeyIndex],
						DependencyHash = depHash,
						DataOffset = bucket.DataOffset,
						Keys = keys,
						ResourceTypeIndex = resTypeIndex
					};

					entryLookup[internalID] = Entries[i];
				}
			}
		}
	}
}