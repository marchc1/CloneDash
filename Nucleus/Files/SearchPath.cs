using System.Text;

namespace Nucleus.Files;

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
	public abstract bool CheckFile(string path, FileAccess? specificAccess = null, FileMode? specificMode = null);
	protected abstract bool CheckDirectory(string path, FileAccess? specificAccess = null, FileMode? specificMode = null);
	/// <summary>
	/// Actually opens a stream. Note: The implementer is responsible for making sure that <see cref="CheckFile(string, FileAccess?, FileMode?)"/> returns
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
	/// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with no access/mode arguments.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public bool DirectoryExists(string path) => CheckDirectory(path);
	/// <summary>
	/// Can the path be read?
	/// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with no access/mode arguments.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public bool Exists(string path) => CheckFile(path);
	/// <summary>
	/// Can the path be read?
	/// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with <see cref="FileAccess.Read"/> and <see cref="FileMode.Open"/>.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public bool CanRead(string path) => CheckFile(path, FileAccess.Read, FileMode.Open);
	/// <summary>
	/// Can the path be read?
	/// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with <see cref="FileAccess.Write"/> and <see cref="FileMode.Create"/>.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public bool CanWrite(string path) => CheckFile(path, FileAccess.Write, FileMode.Create);

	public Stream? Open(string path, FileAccess access, FileMode open) {
		if (!CheckFile(path))
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
