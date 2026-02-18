using AssetStudio;

using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using CommunityToolkit.HighPerformance;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;

using Newtonsoft.Json;

using Nucleus;
using Nucleus.Audio;
using Nucleus.Extensions;

using System.Text;
using System.Text.RegularExpressions;
using ImageFormat = Nucleus.Common.Graphics.ImageFormat;

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
		if (tex2D == null)
			return default;
		var imgData = tex2D.image_data.GetData();
		int width = tex2D.m_Width;
		int height = tex2D.m_Height;
		ImageFormat pixelFormat;
		Raylib_cs.Image img;
		switch (tex2D.m_TextureFormat) {
			case TextureFormat.RGB24:  pixelFormat = ImageFormat.R8G8B8; break;
			case TextureFormat.RGBA32: pixelFormat = ImageFormat.R8G8B8A8; break;
			case TextureFormat.DXT3:   pixelFormat = ImageFormat.DXT3_RGBA; break;
			case TextureFormat.DXT5:   pixelFormat = ImageFormat.DXT5_RGBA; break;
			case TextureFormat.BC4:
			case TextureFormat.BC5:
			case TextureFormat.BC6H:
			case TextureFormat.BC7:
				BcDecoder decoder = new BcDecoder();
				var rgba32 = decoder.DecodeRaw(imgData, width, height, tex2D.m_TextureFormat switch {
					TextureFormat.BC4 => BCnEncoder.Shared.CompressionFormat.Bc4,
					TextureFormat.BC5 => BCnEncoder.Shared.CompressionFormat.Bc5,
					TextureFormat.BC6H => BCnEncoder.Shared.CompressionFormat.Bc6U,
					TextureFormat.BC7 => BCnEncoder.Shared.CompressionFormat.Bc7,
					_ => throw new InvalidOperationException()
				});

				img = rgba32.AsSpan().Cast<ColorRgba32, byte>().ToImage(width, height, ImageFormat.R8G8B8A8, tex2D.m_MipCount);
				return img;
			default: throw new NotImplementedException($"Cannot load the Unity texture format '{tex2D.m_TextureFormat}' into Raylib. Must provide a direct enum conversion or pixel format conversion in UnityAssetUtils.ToRaylib(this Texture2D).");
		}
		img = imgData.ToImage(width, height, pixelFormat, tex2D.m_MipCount);
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
}
