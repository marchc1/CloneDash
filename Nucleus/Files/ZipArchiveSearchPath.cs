using System;
using System.IO.Compression;

namespace Nucleus.Files;

public class ZipArchiveSearchPath : SearchPath
{
	private Dictionary<string, string> LocalToAbsolute = [];
	private HashSet<string> LocalExists = [];
	private ZipArchive archive;
	private bool disposedValue;
	private string rootArchive;
	public override string ToString() {
		return $"ZIP Archive SearchPath @ {rootArchive}";
	}
	public ZipArchiveSearchPath(string rootArchive) {
		this.rootArchive = rootArchive;
		archive = new ZipArchive(new FileStream(rootArchive, FileMode.Open), ZipArchiveMode.Read, false);
	}
	public ZipArchiveSearchPath(string pathID, string path) {
		this.rootArchive = $"(disk path, {pathID}/{path})";
		var stream = Filesystem.Open(pathID, path, FileAccess.Read, FileMode.Open);

		archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
	}

	private string FullNameOf(ZipArchiveEntry entry) => entry.FullName.Replace("\\", "/");

	public override bool CheckFile(ReadOnlySpan<char> path, FileAccess? specificAccess, FileMode? specificMode) {
        for (int i = 0; i < archive.Entries.Count; i++) {
            var entry = archive.Entries[i];
            if (FullNameOf(entry).Equals(path, StringComparison.Ordinal))
                return true;
        }
        return false;
	}

	protected override bool CheckDirectory(ReadOnlySpan<char> path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
		for (int i = 0; i < archive.Entries.Count; i++) {
			var entry = archive.Entries[i];
			if (FullNameOf(entry).StartsWith(path))
				return true;
		}
		return false;
	}

	protected override Stream? OnOpen(ReadOnlySpan<char> path, FileAccess access, FileMode open) {
		// Just in case something *really* goes wrong, a try-catch is done here
		try {
			return archive.GetEntry(new(path))?.Open();
		}
		catch (Exception ex) {
			Logs.Warn($"Core.DiskSearchPath: FileStream errored despite Check succeeding. Reason: {ex.Message}");
			return null;
		}
	}

	public override IEnumerable<string> FindFiles(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}
	public override IEnumerable<string> FindDirectories(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
		throw new NotImplementedException();
	}
}