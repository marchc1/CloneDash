using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nucleus.Files;

public class DiskSearchPath : SearchPath
{
    public string RootDirectory;
    private Dictionary<string, string> LocalToAbsolute = [];
    private HashSet<string> LocalExists = [];
    private bool ReadOnly;
    public override string ToString() {
        return $"Disk SearchPath @ {RootDirectory}";
    }

    void TryCreateDirectory(bool createIfMissing = true) {
        try {
            if (!Directory.Exists(RootDirectory) && createIfMissing)
                Directory.CreateDirectory(RootDirectory);
        }
        catch (Exception e) {
            Logs.Warn($"Could not create directory for {RootDirectory}.");
            Logs.Warn($"    Error:   {e.GetType().Name}");
            Logs.Warn($"    Message: {e.Message}");
        }
    }

    public DiskSearchPath(ReadOnlySpan<char> rootDirectory) {
        RootDirectory = new(rootDirectory);
        TryCreateDirectory();
    }

    public DiskSearchPath MakeReadOnly() {
        ReadOnly = true;
        return this;
    }

    /// <summary>
    /// <b>Note:</b> <paramref name="root"/> must be a <see cref="DiskSearchPath"/>.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="rootDirectory"></param>
    /// <exception cref="NotSupportedException"></exception>
    public DiskSearchPath(SearchPath root, ReadOnlySpan<char> rootDirectory, bool createIfMissing = true) {
        if (root is not DiskSearchPath dsp)
            throw new NotSupportedException("Cannot use a non-DiskSearchPath as a root.");

        RootDirectory = Path.Combine(dsp.RootDirectory.TrimEnd('/').TrimEnd('\\'), new(rootDirectory.TrimStart('/').TrimStart('\\').TrimEnd('/').TrimEnd('\\')));
        TryCreateDirectory(createIfMissing);
    }
    public bool Exists() => Directory.Exists(RootDirectory);

    public static DiskSearchPath Combine(SearchPath root, ReadOnlySpan<char> rootDirectory, bool createIfMissing = true) => new DiskSearchPath(root, rootDirectory, createIfMissing);

    public string ResolveToAbsolute(ReadOnlySpan<char> localPath) {
        return Path.Combine(RootDirectory, new(localPath.TrimStart('/').TrimStart('\\')));
    }
    public string ResolveToLocal(ReadOnlySpan<char> absPath) {
        return Path.GetRelativePath(RootDirectory, new(absPath));
    }

    public override bool CheckFile(ReadOnlySpan<char> path, FileAccess? specificAccess, FileMode? specificMode) {
        if (path.IsEmpty) return false;
        var absPath = ResolveToAbsolute(path);

        var info = new FileInfo(absPath);

        if (specificAccess.HasValue) {
            var access = specificAccess.Value;

            if (access.HasFlag(FileAccess.Read)) {
                if (!info.Exists) return false;
            }

            if (access.HasFlag(FileAccess.Write)) {
                if (ReadOnly)
                    return false;

                if (info.Exists && info.IsReadOnly) return false;
            }

            return true;
        }
        else
            return info.Exists;
    }

    protected override bool CheckDirectory(ReadOnlySpan<char> path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
        var info = new DirectoryInfo(ResolveToAbsolute(path));
        if (!info.Exists) return false;
        return true;
    }

    protected override Stream? OnOpen(ReadOnlySpan<char> path, FileAccess access, FileMode open) {
        // Just in case something *really* goes wrong, a try-catch is done here
        try {
            var absPath = ResolveToAbsolute(path);
            var absFolder = Path.GetDirectoryName(absPath);
            if (access.HasFlag(FileAccess.Write) && absFolder != null && !Directory.Exists(absFolder))
                Directory.CreateDirectory(absFolder);
            return new FileStream(absPath, open, access);
        }
        catch (Exception ex) {
            Logs.Warn($"Core.DiskSearchPath: FileStream errored despite Check succeeding. Reason: {ex.Message}");
            return null;
        }
    }

    // These methods are separated due to compiler complaints about ref-types being preserved across yield boundaries
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerable<string> findFiles(string absPath, string searchQueryStr, SearchOption options) {
        if (Directory.Exists(absPath))
            foreach (var file in Directory.GetFiles(absPath, searchQueryStr, options))
                yield return ResolveToLocal(file);
    }
    public override IEnumerable<string> FindFiles(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
        string absPath = ResolveToAbsolute(path);
        string searchQueryStr = new(searchQuery);
        return findFiles(absPath, searchQueryStr, options);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerable<string> findDirectories(string absPath, string searchQueryStr, SearchOption options) {
        if (Directory.Exists(absPath))
            foreach (var file in Directory.GetDirectories(absPath, searchQueryStr, options))
                yield return ResolveToLocal(file);
    }
    public override IEnumerable<string> FindDirectories(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
        string absPath = ResolveToAbsolute(path);
        string searchQueryStr = new(searchQuery);
        return findDirectories(absPath, searchQueryStr, options);
    }
}
