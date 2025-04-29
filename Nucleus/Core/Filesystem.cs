using Nucleus.Util;
using Raylib_cs;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;

namespace Nucleus.Core
{
	/// <summary>
	/// Base search path that allows multiple different ways to read data from disk, ZIP files, etc...
	/// </summary>
	public abstract class SearchPath
	{
		/// <summary>
		/// Allows the implementer to check if a path is accessible or not, given specific access flags.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="specificAccess"></param>
		/// <param name="specificMode"></param>
		/// <returns></returns>
		public abstract bool CheckFileExists(string path, FileAccess? specificAccess = null, FileMode? specificMode = null);
		protected abstract bool CheckDirectoryExists(string path, FileAccess? specificAccess = null, FileMode? specificMode = null);
		/// <summary>
		/// Actually opens a stream. Note: The implementer is responsible for making sure that <see cref="CheckFileExists(string, FileAccess?, FileMode?)"/> returns
		/// false if the stream cannot be opened. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="access"></param>
		/// <param name="open"></param>
		/// <returns></returns>
		protected abstract Stream? OnOpen(string path, FileAccess access, FileMode open);
		public abstract IEnumerable<string> FindFiles(string path, string searchQuery, SearchOption options);
		public abstract IEnumerable<string> FindDirectories(string path, string searchQuery, SearchOption options);


		/// <summary>
		/// Can the path be read?
		/// <br/><b>Note:</b> A macro to <see cref="CheckFileExists"/> with no access/mode arguments.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool DirectoryExists(string path) => CheckDirectoryExists(path);
		/// <summary>
		/// Can the path be read?
		/// <br/><b>Note:</b> A macro to <see cref="CheckFileExists"/> with no access/mode arguments.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool Exists(string path) => CheckFileExists(path);
		/// <summary>
		/// Can the path be read?
		/// <br/><b>Note:</b> A macro to <see cref="CheckFileExists"/> with <see cref="FileAccess.Read"/> and <see cref="FileMode.Open"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool CanRead(string path) => CheckFileExists(path, FileAccess.Read, FileMode.Open);
		/// <summary>
		/// Can the path be read?
		/// <br/><b>Note:</b> A macro to <see cref="CheckFileExists"/> with <see cref="FileAccess.Write"/> and <see cref="FileMode.Create"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool CanWrite(string path) => CheckFileExists(path, FileAccess.Write, FileMode.Create);

		public Stream? Open(string path, FileAccess access, FileMode open) {
			if (!CheckFileExists(path))
				return null;

			var stream = OnOpen(path, access, open);
			if (stream == null) return null;
			return stream;
		}

		public string? ReadText(string path) {
			if (!CanRead(path)) return null;
			using (var stream = Open(path, FileAccess.Read, FileMode.Open)) {
				if (stream == null) return null;

				using (var reader = new StreamReader(stream)) {
					return reader.ReadToEnd();
				}
			}
		}

		public byte[]? ReadBytes(string path) {
			if (!CanRead(path)) return null;
			using (var stream = Open(path, FileAccess.Read, FileMode.Open)) {
				if (stream == null) return null;

				byte[] buffer = new byte[stream.Length];
				int read = stream.Read(buffer);
				return buffer;
			}
		}

		public bool WriteText(string path, string text) => WriteText(path, text, Encoding.Default);
		public bool WriteText(string path, string text, Encoding encoding) => WriteBytes(path, encoding.GetBytes(text));
		public bool WriteBytes(string path, byte[] data) {
			if (!CanWrite(path)) return false;

			using (var stream = Open(path, FileAccess.Write, FileMode.Create)) {
				if (stream == null) return false;
				stream.Write(data);
				return true;
			}
		}
	}
	public class DiskSearchPath : SearchPath
	{
		public string RootDirectory;
		private Dictionary<string, string> LocalToAbsolute = [];
		private HashSet<string> LocalExists = [];

		public DiskSearchPath(string rootDirectory) {
			RootDirectory = rootDirectory;
			Directory.CreateDirectory(rootDirectory);
		}
		/// <summary>
		/// <b>Note:</b> <paramref name="root"/> must be a <see cref="DiskSearchPath"/>.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="rootDirectory"></param>
		/// <exception cref="NotSupportedException"></exception>
		public DiskSearchPath(SearchPath root, string rootDirectory) {
			if (root is not DiskSearchPath dsp)
				throw new NotSupportedException("Cannot use a non-DiskSearchPath as a root.");
			RootDirectory = Path.Combine(dsp.RootDirectory.TrimEnd('/').TrimEnd('\\'), rootDirectory.TrimStart('/').TrimStart('\\').TrimEnd('/').TrimEnd('\\'));
		}

		public static DiskSearchPath Combine(SearchPath root, string rootDirectory) => new DiskSearchPath(root, rootDirectory);

		public string ResolveToAbsolute(string localPath) {
			return Path.Combine(RootDirectory, localPath.TrimStart('/').TrimStart('\\'));
		}
		public string ResolveToLocal(string absPath) {
			return Path.GetRelativePath(RootDirectory, absPath);
		}

		public override bool CheckFileExists(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
			if (path == null) return false;
			var absPath = ResolveToAbsolute(path);

			var info = new FileInfo(absPath);

			if (!info.Exists) return false;
			if (info.IsReadOnly && specificAccess.HasValue && specificAccess.Value.HasFlag(FileAccess.Write)) return false;
			return true;
		}

		protected override bool CheckDirectoryExists(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
			var info = new DirectoryInfo(ResolveToAbsolute(path));
			if (!info.Exists) return false;
			return true;
		}

		protected override Stream? OnOpen(string path, FileAccess access, FileMode open) {
			// Just in case something *really* goes wrong, a try-catch is done here
			try {
				return new FileStream(ResolveToAbsolute(path), open, access);
			}
			catch (Exception ex) {
				Logs.Warn($"Core.DiskSearchPath: FileStream errored despite Check succeeding. Reason: {ex.Message}");
				return null;
			}
		}

		public override IEnumerable<string> FindFiles(string path, string searchQuery, SearchOption options) {
			string absPath = ResolveToAbsolute(path);
			if (Directory.Exists(absPath))
				foreach (var file in Directory.GetFiles(absPath, searchQuery, options)) {
					yield return ResolveToLocal(file);
				}
		}
		public override IEnumerable<string> FindDirectories(string path, string searchQuery, SearchOption options) {
			string absPath = ResolveToAbsolute(path);
			if (Directory.Exists(absPath))
				foreach (var file in Directory.GetDirectories(absPath, searchQuery, options)) {
					yield return ResolveToLocal(file);
				}
		}
	}
	public class ZipArchiveSearchPath : SearchPath
	{
		private Dictionary<string, string> LocalToAbsolute = [];
		private HashSet<string> LocalExists = [];
		private ZipArchive archive;

		public ZipArchiveSearchPath(string rootArchive) {
			archive = new ZipArchive(new FileStream(rootArchive, FileMode.Open), ZipArchiveMode.Read, false);
		}
		public ZipArchiveSearchPath(string pathID, string path) {
			archive = new ZipArchive(Filesystem.Open(pathID, path, FileAccess.Read, FileMode.Open), ZipArchiveMode.Read, false);
		}

		private string FullNameOf(ZipArchiveEntry entry) => entry.FullName.Replace("\\", "/");

		public override bool CheckFileExists(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
			return archive.Entries.FirstOrDefault(x => FullNameOf(x) == path) != null;
		}

		protected override bool CheckDirectoryExists(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
			return archive.Entries.FirstOrDefault(x => FullNameOf(x).StartsWith(path)) != null;
		}

		protected override Stream? OnOpen(string path, FileAccess access, FileMode open) {
			// Just in case something *really* goes wrong, a try-catch is done here
			try {
				return archive.GetEntry(path)?.Open();
			}
			catch (Exception ex) {
				Logs.Warn($"Core.DiskSearchPath: FileStream errored despite Check succeeding. Reason: {ex.Message}");
				return null;
			}
		}

		public override IEnumerable<string> FindFiles(string path, string searchQuery, SearchOption options) {
			throw new NotImplementedException();
		}
		public override IEnumerable<string> FindDirectories(string path, string searchQuery, SearchOption options) {
			throw new NotImplementedException();
		}
	}
	public enum SearchPathAdd
	{
		/// <summary>
		/// Adds the search path to the start; ie. it will be searched first.
		/// </summary>
		ToHead,

		/// <summary>
		/// Adds the search path to the end; ie. it will be searched last.
		/// </summary>
		ToTail
	};

	public class SearchPathID : List<SearchPath>;

	public static class Filesystem
	{
		//public static Dictionary<string, List<string>> Path { get; } = new() {
		//	{ "game", [$"{AppContext.BaseDirectory}"] },
		//	{ "assets", [$"{AppContext.BaseDirectory}assets"] },

		//	{ "audio", [$"{AppContext.BaseDirectory}assets/audio"] },
		//	{ "cfg", [$"{AppContext.BaseDirectory}cfg"] },
		//	{ "fonts", [$"{AppContext.BaseDirectory}assets/fonts"] },
		//	{ "images", [$"{AppContext.BaseDirectory}assets/images"] },
		//	{ "models", [$"{AppContext.BaseDirectory}assets/models"] },
		//	{ "shaders", [$"{AppContext.BaseDirectory}assets/shaders"] },


		//};

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

			var pathObj = AddSearchPath<T>(pathID, path, add);
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
}
