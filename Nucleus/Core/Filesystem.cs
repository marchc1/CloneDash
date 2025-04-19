using Nucleus.Util;
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
		public abstract bool Check(string path, FileAccess? specificAccess = null, FileMode? specificMode = null);
		/// <summary>
		/// Actually opens a stream. Note: The implementer is responsible for making sure that <see cref="Check(string, FileAccess?, FileMode?)"/> returns
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
		/// <br/><b>Note:</b> A macro to <see cref="Check"/> with no access/mode arguments.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool Exists(string path) => Check(path);
		/// <summary>
		/// Can the path be read?
		/// <br/><b>Note:</b> A macro to <see cref="Check"/> with <see cref="FileAccess.Read"/> and <see cref="FileMode.Open"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool CanRead(string path) => Check(path, FileAccess.Read, FileMode.Open);
		/// <summary>
		/// Can the path be read?
		/// <br/><b>Note:</b> A macro to <see cref="Check"/> with <see cref="FileAccess.Write"/> and <see cref="FileMode.Create"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool CanWrite(string path) => Check(path, FileAccess.Write, FileMode.Create);

		public Stream? Open(string path, FileAccess access, FileMode open) {
			if (!Check(path))
				return null;

			var stream = OnOpen(path, access, open);
			if (stream == null) return null;
			return stream;
		}

		public string? ReadText(string path) => ReadText(path, Encoding.UTF8);
		public string? ReadText(string path, Encoding encoding) {
			var bytes = ReadBytes(path);
			if (bytes == null) return null;

			return encoding.GetString(bytes);
		}

		public byte[]? ReadBytes(string path) {
			if (!CanRead(path)) return null;
			using (var stream = Open(path, FileAccess.Read, FileMode.Open)) {
				if (stream == null) return null;

				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer);
				return buffer;
			}
		}

		public bool WriteText(string path, string text) => WriteText(path, text, Encoding.UTF8);
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

		private string resolveToAbsolute(string localPath) {
			return Path.Combine(RootDirectory, localPath.TrimStart('/').TrimStart('\\'));
		}
		private string resolveToLocal(string absPath) {
			return Path.GetRelativePath(RootDirectory, absPath);
		}

		protected override bool Check(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
			if (path == null) return false;
			var absPath = resolveToAbsolute(path);

			var info = new FileInfo(path);

			if (!info.Exists) return false;
			if (info.IsReadOnly && specificAccess.HasValue && specificAccess.Value.HasFlag(FileAccess.Write)) return false;
			return true;
		}

		protected override Stream? OnOpen(string path, FileAccess access, FileMode open) {
			// Just in case something *really* goes wrong, a try-catch is done here
			try {
				return new FileStream(resolveToAbsolute(path), open, access);
			}
			catch (Exception ex) {
				Logs.Warn($"Core.DiskSearchPath: FileStream errored despite Check succeeding. Reason: {ex.Message}");
				return null;
			}
		}

		public override IEnumerable<string> FindFiles(string path, string searchQuery, SearchOption options) {
			foreach (var file in Directory.GetFiles(resolveToAbsolute(path), searchQuery, options)) {
				yield return resolveToLocal(file);
			}
		}
		public override IEnumerable<string> FindDirectories(string path, string searchQuery, SearchOption options) {
			foreach (var file in Directory.GetDirectories(resolveToAbsolute(path), searchQuery, options)) {
				yield return resolveToLocal(file);
			}
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
				var assets = AddSearchPath("assets", DiskSearchPath.Combine(game, "assets"));
				{
					var audio = AddSearchPath("audio", DiskSearchPath.Combine(assets, "audio"));
					var cfg = AddSearchPath("cfg", DiskSearchPath.Combine(assets, "cfg"));
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
			level.Unloaded += () => RemoveSearchPath(pathID, pathObj);
			return pathObj;
		}

		public static bool RemoveSearchPath(string pathID, SearchPath path) {
			if (!Path.TryGetValue(pathID, out var pathIDObj)) return false;
			return pathIDObj.Remove(path);
		}


		public static IEnumerable<string> FindFiles(string path, string searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly) {
			foreach (var pathID in GetSearchPathID(path))
				foreach (var file in pathID.FindFiles(path, searchPattern, searchOptions))
					yield return file;
		}

		public static IEnumerable<string> FindDirectories(string path, string searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly) {
			foreach (var pathID in GetSearchPathID(path))
				foreach (var file in pathID.FindDirectories(path, searchPattern, searchOptions))
					yield return file;
		}

		public static Stream? Open(string pathID, string path, FileAccess access = FileAccess.ReadWrite, FileMode mode = FileMode.OpenOrCreate) {
			foreach (var pathObj in GetSearchPathID(pathID)) {
				if (pathObj.Check(path, access, mode)) {
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

		public static string? ReadAllText(string pathID, string path, Encoding encoding) {
			foreach (var pathObj in GetSearchPathID(pathID)) {
				var text = pathObj.ReadText(path, encoding);
				if (text != null) return text;
			}

			return null;
		}

		public static byte[]? ReadAllBytes(string pathID, string path, Encoding encoding) {
			foreach (var pathObj in GetSearchPathID(pathID)) {
				var text = pathObj.ReadBytes(path);
				if (text != null) return text;
			}

			return null;
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
	}
}
