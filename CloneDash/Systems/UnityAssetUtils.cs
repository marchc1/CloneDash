using AssetStudio;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp;
using Newtonsoft.Json;
using Nucleus.Audio;
using Nucleus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace CloneDash.Systems
{
	/// <summary>
	/// Various Unity asset loading utility methods. Tries to abstract away a lot of the AssetStudio stuff.
	/// </summary>
	public static class UnityAssetUtils
	{
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
		private static AssetType __internalLoadAsset<AssetType>(string[] streamingFiles, string query, bool regex = false) {
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

		/// <summary>
		/// Method for struct types.
		/// </summary>
		public static ReturnStructure LoadAssetEasyS<AssetType, ReturnStructure>(string[] streamingFiles, string query, bool regex = false) where AssetType : class where ReturnStructure : struct {
			AssetType item = __internalLoadAsset<AssetType>(streamingFiles, query, regex);

			switch (item) {
				case Texture2D texture2D:
					var imgData = AssetStudio.Texture2DExtensions.ConvertToStream(texture2D, ImageFormat.Png, true).ToArray();
					var img = Raylib_cs.Raylib.LoadImageFromMemory(".png", imgData);
					var tex = Raylib_cs.Raylib.LoadTextureFromImage(img);
					return (ReturnStructure)(object)tex;
				default:
					throw new NotImplementedException($"There is not a struct ReturnStructure generator for {typeof(AssetType).Name}!");
			}
		}
		/// <summary>
		/// Method for class-types.
		/// </summary>
		public static ReturnStructure LoadAssetEasyC<AssetType, ReturnStructure>(string[] streamingFiles, string query, bool regex = false) where AssetType : class where ReturnStructure : class {
			AssetType item = __internalLoadAsset<AssetType>(streamingFiles, query, regex);

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
}
