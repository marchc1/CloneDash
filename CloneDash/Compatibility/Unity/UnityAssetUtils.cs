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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using ImageFormat = Nucleus.Common.Graphics.ImageFormat;

namespace CloneDash.Compatibility.Unity;

public class MonoBehaviourReader : IEnumerable<KeyValuePair<object, object?>>
{
	public readonly MonoBehaviour MonoBehaviour;
	public readonly OrderedDictionary Dict;
	public MonoBehaviourReader(MonoBehaviour mb) {
		MonoBehaviour = mb;
		Dict = mb.ToType();
	}

	public T? GetAny<T>(object key) {
		object? o = Dict[key];
		if (o == null) return default;

		return (T?)o;
	}

	public T? Get<T>(object key) where T : AssetStudio.Object {
		object? o = Dict[key];
		if (o == null) return null;

		return (T?)GetUnderlyingTypeByNecessaryMeans(o);
	}


	public List<T?> GetList<T>(object key) where T : AssetStudio.Object {
		object? o = Dict[key];
		if (o == null) return [];

		if (o is not List<object> baseList) return [(T?)GetUnderlyingTypeByNecessaryMeans(o)!];

		List<T?> ret = [];
		ret.EnsureCapacity(baseList.Count);

		foreach(var kvp in baseList)
			ret.Add((T?)GetUnderlyingTypeByNecessaryMeans(kvp));
		
		return ret;
	}

	public MonoBehaviourReader? GetMB(object key) {
		object? o = Dict[key];
		if (o == null) return null;
		if (o is not PPtr<MonoBehaviour> pMb) return null;
		return pMb.TryGet(out var mb) ? new(mb) : null;
	}

	object? GetUnderlyingTypeByNecessaryMeans(object? value) {
		if (value == null)
			return null;

		IPPtr ptr;
		switch (value) {
			case OrderedDictionary potentialPtr: {
					if (potentialPtr.Count != 2)
						return null;

					object[] keys = new object[2];
					object[] values = new object[2];
					potentialPtr.Keys.CopyTo(keys, 0);
					if (keys[0] is not string k1 || keys[1] is not string k2)
						return null;

					potentialPtr.Values.CopyTo(values, 0);
					if (k1 == "m_FileID" && k2 == "m_PathID") {
						ptr = new PPtr<AssetStudio.Object>((int)values[0], (long)values[1], MonoBehaviour.assetsFile);
						goto gotAPtr;
					}
					if (k1 == "m_PathID" && k2 == "m_FileID") {
						ptr = new PPtr<AssetStudio.Object>((int)values[1], (long)values[0], MonoBehaviour.assetsFile);
						goto gotAPtr;
					}

					return null;
				}
			case IPPtr vptr:
				ptr = vptr;
			gotAPtr:
				return ptr.TryGet(out AssetStudio.Object? o) ? o : null;
			default:
				return value;
		}
	}

	public IEnumerator<KeyValuePair<object, object?>> GetEnumerator() {
		foreach (var kvp in Dict.Keys) {
			yield return new(kvp, GetUnderlyingTypeByNecessaryMeans(Dict[kvp]));
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Various Unity asset loading utility methods. Tries to abstract away a lot of the AssetStudio stuff.
/// </summary>
public static class UnityAssetUtils
{
	/// <summary>
	/// Gets the name of various descendants of <see cref="AssetStudio.Object"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static string? GetUnityName<T>(this T obj) where T : AssetStudio.Object {
		switch (obj) {
			case MonoBehaviour mb: return mb.m_Name;
			case GameObject go: return go.m_Name;
			case NamedObject no: return no.m_Name;
			default: return null;
		}
	}
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
			case TextureFormat.RGB24: pixelFormat = ImageFormat.R8G8B8; break;
			case TextureFormat.RGBA32: pixelFormat = ImageFormat.R8G8B8A8; break;
			case TextureFormat.DXT3: pixelFormat = ImageFormat.DXT3_RGBA; break;
			case TextureFormat.DXT5: pixelFormat = ImageFormat.DXT5_RGBA; break;
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
