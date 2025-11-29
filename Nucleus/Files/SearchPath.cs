using System.IO;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public abstract bool CheckFile(ReadOnlySpan<char> path, FileAccess? specificAccess, FileMode? specificMode);
    protected abstract bool CheckDirectory(ReadOnlySpan<char> path, FileAccess? specificAccess = null, FileMode? specificMode = null);
    /// <summary>
    /// Actually opens a stream. Note: The implementer is responsible for making sure that <see cref="CheckFile(string, FileAccess?, FileMode?)"/> returns
    /// false if the stream cannot be opened. 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="access"></param>
    /// <param name="open"></param>
    /// <returns></returns>
    protected abstract Stream? OnOpen(ReadOnlySpan<char> path, FileAccess access, FileMode open);
    public abstract IEnumerable<string> FindFiles(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options);
    public abstract IEnumerable<string> FindDirectories(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options);


    /// <summary>
    /// Can the path be read?
    /// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with no access/mode arguments.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool DirectoryExists(ReadOnlySpan<char> path) => CheckDirectory(path);
    /// <summary>
    /// Can the path be read?
    /// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with no access/mode arguments.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool Exists(ReadOnlySpan<char> path) => CheckFile(path, null, null);
    /// <summary>
    /// Can the path be read?
    /// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with <see cref="FileAccess.Read"/> and <see cref="FileMode.Open"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool CanRead(ReadOnlySpan<char> path) => CheckFile(path, FileAccess.Read, FileMode.Open);
    /// <summary>
    /// Can the path be read?
    /// <br/><b>Note:</b> A macro to <see cref="CheckFile"/> with <see cref="FileAccess.Write"/> and <see cref="FileMode.Create"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool CanWrite(ReadOnlySpan<char> path) => CheckFile(path, FileAccess.Write, FileMode.Create);

    public Stream? Open(ReadOnlySpan<char> path, FileAccess access, FileMode open) {
        if (!CheckFile(path, access, open))
            return null;

        var stream = OnOpen(path, access, open);
        if (stream == null) return null;
        return stream;
    }

    public string? ReadText(ReadOnlySpan<char> path) {
        if (!CanRead(path)) return null;
        using (var stream = Open(path, FileAccess.Read, FileMode.Open)) {
            if (stream == null) return null;

            using (var reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }

    public byte[]? ReadBytes(ReadOnlySpan<char> path) {
        if (!CanRead(path)) return null;
        using (var stream = Open(path, FileAccess.Read, FileMode.Open)) {
            if (stream == null) return null;

            byte[] buffer = new byte[stream.Length];
            int read = stream.Read(buffer);
            return buffer;
        }
    }

    public bool WriteText(ReadOnlySpan<char> path, ReadOnlySpan<char> text) => WriteText(path, text, Encoding.Default);
    readonly ThreadLocal<byte[]> localTextBuffs = new(() => new byte[1024 * 1024]);
    public bool WriteText(ReadOnlySpan<char> path, ReadOnlySpan<char> text, Encoding encoding) {
        if (!CanWrite(path)) return false;
        using (var stream = Open(path, FileAccess.Write, FileMode.Create)) {
            if (stream == null) return false;
            var scratchBuffer = localTextBuffs.Value!;
            int bytesRequired = encoding.GetByteCount(text);

            if (scratchBuffer.Length < bytesRequired) {
                int oldLength = scratchBuffer.Length;
                int newLength = oldLength;
                while (newLength < bytesRequired)
                    newLength = newLength * 2;
                scratchBuffer = new byte[newLength];
                localTextBuffs.Value = scratchBuffer;
                Logs.Info($"FileSystem.WriteText: incremented thread #{Thread.CurrentThread.ManagedThreadId}'s scratchbuffer from {oldLength} -> {newLength} bytes");
            }
            encoding.GetBytes(text, scratchBuffer);
            stream.Write(scratchBuffer.AsSpan()[..bytesRequired]);
            return true;
        }

    }
    public bool WriteBytes(ReadOnlySpan<char> path, ReadOnlySpan<byte> data) {
        if (!CanWrite(path)) return false;

        using (var stream = Open(path, FileAccess.Write, FileMode.Create)) {
            if (stream == null) return false;
            stream.Write(data);
            return true;
        }
    }
}
