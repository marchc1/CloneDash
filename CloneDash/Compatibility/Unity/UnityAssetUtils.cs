using AssetStudio;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp;
using Newtonsoft.Json;
using Nucleus.Audio;
using Nucleus;
using System.Text;
using System.Text.RegularExpressions;
using BCnEncoder.Decoder;
using Nucleus.Extensions;

namespace CloneDash.Compatibility.Unity;

/// <summary>
/// Various Unity asset loading utility methods. Tries to abstract away a lot of the AssetStudio stuff.
/// </summary>
public static class UnityAssetUtils
{
	/// <summary>
	/// Note this returns a Raylib image, you will need to manually unload the pixel data...
	/// </summary>
	/// <param name="tex2D"></param>
	/// <returns></returns>
	public static Raylib_cs.Image ToRaylib(this Texture2D tex2D) {
		var imgData = tex2D.image_data.GetData();
		int width = tex2D.m_Width;
		int height = tex2D.m_Height;

		Raylib_cs.PixelFormat pixelFormat;
		switch (tex2D.m_TextureFormat) {
			case TextureFormat.DXT5: pixelFormat = Raylib_cs.PixelFormat.PIXELFORMAT_COMPRESSED_DXT5_RGBA; break;
			case TextureFormat.BC7:
				BcDecoder decoder = new BcDecoder();
				var rgba32 = decoder.DecodeRaw(imgData, width, height, BCnEncoder.Shared.CompressionFormat.Bc7);

				imgData = new byte[rgba32.Length * 4];
				for (int i = 0; i < rgba32.Length; i += 1) {
					var px = rgba32[i];
					var imgDataPtr = i * 4;
					imgData[imgDataPtr] = px.r;
					imgData[imgDataPtr + 1] = px.g;
					imgData[imgDataPtr + 2] = px.b;
					imgData[imgDataPtr + 3] = px.a;
				}
				pixelFormat = Raylib_cs.PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;
				break;
			default: throw new NotImplementedException($"Cannot load the Unity texture format '{tex2D.m_TextureFormat}' into Raylib. Must provide a direct enum conversion or pixel format conversion in UnityAssetUtils.ToRaylib(this Texture2D).");
		}
		var img = imgData.ToImage(width, height, pixelFormat, tex2D.m_MipCount);
		return img;
	}
	private static ClassIDType GetClassIDFromType(Type t) {
		switch (t.Name) {
			case "AudioClip": return ClassIDType.AudioClip;
			case "TextAsset": return ClassIDType.TextAsset;
			case "Texture2D": return ClassIDType.Texture2D;
		}
		return ClassIDType.UnknownType;
	}

	/// <summary>
	/// Internal Unity asset loader. Searches <paramref name="streamingFiles"/> given <paramref name="query"/> and <paramref name="regex"/> and returns an <typeparamref name="AssetType"/> from that.<br></br>
	/// This will load the first item that matches the type; this works for our use cases though
	/// </summary>
	public static AssetType InternalLoadAsset<AssetType>(string[] streamingFiles, string query, bool regex = false) {
		AssetsManager manager = new();
		string? filepath = streamingFiles.First(x => regex ? Regex.IsMatch(x, query) : x.Contains(query));
		if (filepath == null)
			throw new FileNotFoundException($"No file matched the regular expression/query for \"{query}\"");
		manager.LoadFiles(filepath);

		AssetType item = (AssetType)(object)manager.assetsFileList[0].Objects.FirstOrDefault(x => x.type == GetClassIDFromType(typeof(AssetType)));
		if (item == null)
			throw new NotImplementedException($"Could not convert! Is there a type conversion definition for {typeof(AssetType).Name}?");

		return item;
	}

	private static bool testr(string x, string query) {
		return Regex.IsMatch(x, query);
	}
	public static string[] GetAllFiles(string[] streamingFiles, string query, bool regex = false) {
		AssetsManager manager = new();
		var files = streamingFiles.Where(x => regex ? testr(x, query) : x.Contains(query));
		if (!files.Any())
			throw new FileNotFoundException($"No file matched the regular expression/query for \"{query}\"");

		return files.ToArray();
	}
	public static AssetType[] LoadAllAssetsOf<AssetType>(string[] streamingFiles, string query, bool regex = false) {
		AssetsManager manager = new();
		var files = streamingFiles.Where(x => regex ? testr(x, query) : x.Contains(query));
		if (!files.Any())
			throw new FileNotFoundException($"No file matched the regular expression/query for \"{query}\"");

		manager.LoadFiles(files.ToArray());

		var items = manager.assetsFileList[0].Objects.Where(x => x.type == GetClassIDFromType(typeof(AssetType)));
		if (!items.Any())
			throw new NotImplementedException($"Could not convert! Is there a type conversion definition for {typeof(AssetType).Name}?");

		AssetType[] castItems = new AssetType[items.Count()];
		int i = 0;
		foreach (var item in items) {
			castItems[i] = (AssetType)(object)item;
			i++;
		}

		return castItems;
	}

	/// <summary>
	/// Method for class-types.
	/// </summary>
	public static ReturnStructure LoadAssetEasyC<AssetType, ReturnStructure>(string[] streamingFiles, string query, bool regex = false) where AssetType : class where ReturnStructure : class {
		AssetType item = InternalLoadAsset<AssetType>(streamingFiles, query, regex);

		switch (item) {
			case AudioClip audioClip:
				if (typeof(ReturnStructure) != typeof(MusicTrack)) throw new NotImplementedException("AudioClip returns a MusicTrack and cannot return a different type.");

				byte[] musicStream;
				var audiodata = audioClip.m_AudioData.GetData();

				if (audioClip.m_Type == FMODSoundType.UNKNOWN) {
					FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(audiodata);
					bank.Samples[0].RebuildAsStandardFileFormat(out musicStream, out var fileExtension);

					return EngineCore.Level.Sounds.LoadMusicFromMemory(musicStream) as ReturnStructure;
				}

				throw new Exception("Something went wrong loading an AudioClip");
			case TextAsset textAsset:
				return JsonConvert.DeserializeObject<ReturnStructure>(Encoding.UTF8.GetString(textAsset.m_Script));
			default:
				throw new NotImplementedException($"There is not a class ReturnStructure generator for {typeof(AssetType).Name}!");
		}
	}
}
