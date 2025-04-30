using Raylib_cs;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nucleus.Files;

public static class Filesystem
{
	public static Dictionary<string, SearchPathID> Path { get; } = [];

	static Filesystem() {
		var game = AddSearchPath<DiskSearchPath>("game", AppContext.BaseDirectory);
		{
			var cfg = AddSearchPath("cfg", DiskSearchPath.Combine(game, "cfg"));
			var assets = AddSearchPath("assets", DiskSearchPath.Combine(game, "assets"));
			{
				var audio = AddSearchPath("audio", DiskSearchPath.Combine(assets, "audio"));
				var fonts = AddSearchPath("fonts", DiskSearchPath.Combine(assets, "fonts"));
				var images = AddSearchPath("images", DiskSearchPath.Combine(assets, "images"));
				var models = AddSearchPath("models", DiskSearchPath.Combine(assets, "models"));
				var shaders = AddSearchPath("shaders", DiskSearchPath.Combine(assets, "shaders"));
			}
		}
	}

	/// <summary>
	/// Gets, or creates, a <see cref="SearchPathID"/>
	/// </summary>
	/// <param name="pathID"></param>
	/// <returns></returns>
	public static SearchPathID GetSearchPathID(string pathID) {
		if (Path.TryGetValue(pathID, out var pathIDObj))
			return pathIDObj;

		pathIDObj = new();
		Path[pathID] = pathIDObj;

		return pathIDObj;
	}

	public static T AddSearchPath<T>(string pathID, T searchPath, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath {
		var pathIDObj = GetSearchPathID(pathID);
		switch (add) {
			case SearchPathAdd.ToHead:
				pathIDObj.Insert(0, searchPath);
				break;
			case SearchPathAdd.ToTail:
				pathIDObj.Add(searchPath);
				break;
		}
		return searchPath;
	}

	public static T AddSearchPath<T>(string pathID, string path, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath {
		T ret;
		switch (typeof(T).Name) {
			case nameof(DiskSearchPath):
				ret = (T)(object)new DiskSearchPath(path);
				break;
			default: throw new NotImplementedException($"Unknown FileSystem type '{typeof(T).Name}'. Please implement it in Nucleus.Core.FileSystem.");
		}

		AddSearchPath(pathID, ret, add);
		return ret;
	}

	/// <summary>
	/// Adds a search path that is destroyed when the level is deinitialized.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="pathID"></param>
	/// <param name="path"></param>
	/// <param name="add"></param>
	/// <returns></returns>
	public static T AddTemporarySearchPath<T>(string pathID, string path, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath {
		var level = EngineCore.Level ?? throw new NotSupportedException("Cannot create temporary search paths when no level is active.");

		var pathObj = AddSearchPath<T>(pathID, path, add);
		level.AddFinalizer((lvl) => RemoveSearchPath(pathID, pathObj));
		return pathObj;
	}

	public static T AddTemporarySearchPath<T>(string pathID, T path, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath {
		var level = EngineCore.Level ?? throw new NotSupportedException("Cannot create temporary search paths when no level is active.");

		var pathObj = AddSearchPath(pathID, path, add);
		level.AddFinalizer((lvl) => RemoveSearchPath(pathID, pathObj));
		return pathObj;
	}

	public static bool RemoveSearchPath(string pathID, SearchPath path) {
		if (!Path.TryGetValue(pathID, out var pathIDObj)) return false;
		return pathIDObj.Remove(path);
	}

	public static bool RemoveSearchPath(string pathID) {
		if (!Path.TryGetValue(pathID, out var pathIDObj)) return false;
		return pathIDObj.RemoveAll(x => true) > 0;
	}

	public static SearchPath? FindSearchPath(string pathID, string path) {
		foreach (var pathIDObj in GetSearchPathID(pathID))
			if (pathIDObj.Exists(path))
				return pathIDObj;

		return null;
	}

	public static IEnumerable<string> FindFiles(string pathID, string searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly) {
		foreach (var pathIDObj in GetSearchPathID(pathID))
			foreach (var file in pathIDObj.FindFiles("", searchPattern, searchOptions))
				yield return file;
	}

	public static IEnumerable<string> FindDirectories(string pathID, string searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly) {
		foreach (var pathIDObj in GetSearchPathID(pathID))
			foreach (var file in pathIDObj.FindDirectories("", searchPattern, searchOptions))
				yield return file;
	}

	public static IEnumerable<string> FindFiles(string pathID, string path, string searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly) {
		foreach (var pathIDObj in GetSearchPathID(pathID))
			foreach (var file in pathIDObj.FindFiles(path, searchPattern, searchOptions))
				yield return file;
	}

	public static IEnumerable<string> FindDirectories(string pathID, string path, string searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly) {
		foreach (var pathIDObj in GetSearchPathID(pathID))
			foreach (var file in pathIDObj.FindDirectories(path, searchPattern, searchOptions))
				yield return file;
	}

	public static Stream? Open(string pathID, string path, FileAccess access = FileAccess.ReadWrite, FileMode mode = FileMode.OpenOrCreate) {
		foreach (var pathObj in GetSearchPathID(pathID)) {
			if (pathObj.CheckFileExists(path, access, mode)) {
				var stream = pathObj.Open(path, access, mode);
				if (stream != null) return stream;
			}
		}

		return null;
	}

	public static string? ReadAllText(string pathID, string path) {
		foreach (var pathObj in GetSearchPathID(pathID)) {
			var text = pathObj.ReadText(path);
			if (text != null) return text;
		}

		return null;
	}

	public static byte[]? ReadAllBytes(string pathID, string path) {
		foreach (var pathObj in GetSearchPathID(pathID)) {
			var bytes = pathObj.ReadBytes(path);
			if (bytes != null) return bytes;
		}

		return null;
	}

	public static bool ReadAllText(string pathID, string path, [NotNullWhen(true)] out string? text) {
		text = ReadAllText(pathID, path);
		return text != null;
	}

	public static bool ReadAllBytes(string pathID, string path, [NotNullWhen(true)] out byte[]? bytes) {
		bytes = ReadAllBytes(pathID, path);
		return bytes != null;
	}

	public static bool WriteAllText(string pathID, string path, string text, Encoding encoding) {
		foreach (var pathObj in GetSearchPathID(pathID)) {
			var succeeded = pathObj.WriteText(path, text, encoding);
			if (succeeded) return true;
		}

		return false;
	}

	public static bool WriteAllText(string pathID, string path, string text) {
		foreach (var pathObj in GetSearchPathID(pathID)) {
			var succeeded = pathObj.WriteText(path, text);
			if (succeeded) return true;
		}

		return false;
	}

	public static bool WriteAllBytes(string pathID, string path, byte[] data) {
		foreach (var pathObj in GetSearchPathID(pathID)) {
			var succeeded = pathObj.WriteBytes(path, data);
			if (succeeded) return true;
		}

		return false;
	}

	private static FileNotFoundException NotFound(string pathID, string path) => new FileNotFoundException($"Cannot find '{path}' in '{pathID}'!");
	public static string GetExtension(string path) => System.IO.Path.GetExtension(path) ?? "";

	// Extra Raylib macros.

	public static Image ReadImage(string pathID, string path) {
		byte[]? data = ReadAllBytes(pathID, path);
		if (data == null) throw NotFound(pathID, path);
		return Raylib.LoadImageFromMemory(GetExtension(path), data);
	}
	public static Texture2D ReadTexture(string pathID, string path, TextureFilter filter = TextureFilter.TEXTURE_FILTER_BILINEAR) {
		using (Raylib.ImageRef img = new(ReadImage(pathID, path))) {
			var tex = Raylib.LoadTextureFromImage(img);
			Raylib.SetTextureFilter(tex, filter);
			return tex;
		}
	}
	public static Sound ReadSound(string pathID, string path) {
		byte[]? data = ReadAllBytes(pathID, path);
		if (data == null) throw NotFound(pathID, path);
		var wav = Raylib.LoadWaveFromMemory(GetExtension(path), data);
		var snd = Raylib.LoadSoundFromWave(wav);
		Raylib.UnloadWave(wav);
		return snd;
	}
	public static Music ReadMusic(string pathID, string path) {
		byte[]? data = ReadAllBytes(pathID, path);
		if (data == null) throw NotFound(pathID, path);
		var music = Raylib.LoadMusicStreamFromMemory(GetExtension(path), data);
		return music;
	}
	public static Font ReadFont(string pathID, string path, int fontSize, int[] codepoints, int codepointCount) {
		byte[]? data = ReadAllBytes(pathID, path);
		if (data == null) throw NotFound(pathID, path);

		var font = Raylib.LoadFontFromMemory(GetExtension(path), data, fontSize, codepoints, codepointCount);
		return font;
	}
	public static Shader ReadVertexShader(string pathID, string vertexShader) {
		string? data = ReadAllText(pathID, vertexShader);
		if (data == null) throw NotFound(pathID, vertexShader);

		var shader = Raylib.LoadShaderFromMemory(data, null);
		return shader;
	}
	public static Shader ReadFragmentShader(string pathID, string fragmentShader) {
		string? data = ReadAllText(pathID, fragmentShader);
		if (data == null) throw NotFound(pathID, fragmentShader);

		var shader = Raylib.LoadShaderFromMemory(null, data);
		return shader;
	}
	public static Shader ReadShader(string pathID, string vertexShader, string fragmentShader) {
		string? vertexData = ReadAllText(pathID, vertexShader);
		if (vertexData == null) throw NotFound(pathID, vertexShader);

		string? fragmentData = ReadAllText(pathID, fragmentShader);
		if (fragmentData == null) throw NotFound(pathID, fragmentShader);

		var shader = Raylib.LoadShaderFromMemory(vertexData, fragmentData);
		return shader;
	}
}
