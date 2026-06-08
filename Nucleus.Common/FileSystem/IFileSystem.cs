using Nucleus.Files;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nucleus.Common.FileSystem;

public static class FileSystemGlobals
{
	public const FileNameHandle_t FILENAMEHANDLE_INVALID = 0;
}

public interface IFileSystem
{
	T AddSearchPath<T>(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath;
	T AddSearchPath<T>(ReadOnlySpan<char> pathID, T searchPath, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath;
	T AddTemporarySearchPath<T>(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath;
	T AddTemporarySearchPath<T>(ReadOnlySpan<char> pathID, T path, SearchPathAdd add = SearchPathAdd.ToTail) where T : SearchPath;
	bool CanOpen(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, FileAccess access = FileAccess.ReadWrite, FileMode mode = FileMode.OpenOrCreate);
	bool Exists(string pathID, string path);
	IEnumerable<string> FindDirectories(ReadOnlySpan<char> pathID, ReadOnlySpan<char> searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly);
	IEnumerable<string> FindDirectories(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, ReadOnlySpan<char> searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly);
	IEnumerable<string> FindFiles(ReadOnlySpan<char> pathID, ReadOnlySpan<char> searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly);
	IEnumerable<string> FindFiles(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, ReadOnlySpan<char> searchPattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly);
	SearchPath? FindSearchPath(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path);
	IEnumerable<SearchPath> GetSearchPathID(ReadOnlySpan<char> pathID);
	void Initialize(ReadOnlySpan<char> gameName);
	Stream? Open(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, FileAccess access = FileAccess.ReadWrite, FileMode mode = FileMode.OpenOrCreate);
	byte[]? ReadAllBytes(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path);
	bool ReadAllBytes(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, [NotNullWhen(true)] out byte[]? bytes);
	string? ReadAllText(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path);
	bool ReadAllText(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, [NotNullWhen(true)] out string? text);
	bool RemoveSearchPath(ReadOnlySpan<char> pathID);
	bool RemoveSearchPath(ReadOnlySpan<char> pathID, SearchPath path);
	void RemoveTemporarySearchPaths();
	bool WriteAllBytes(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, byte[] data);
	bool WriteAllText(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, ReadOnlySpan<char> text);
	bool WriteAllText(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, ReadOnlySpan<char> text, Encoding encoding);
}