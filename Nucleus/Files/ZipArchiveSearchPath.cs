using System.IO.Compression;

namespace Nucleus.Files;

public class ZipArchiveSearchPath : SearchPath
{
	private Dictionary<string, string> LocalToAbsolute = [];
	private HashSet<string> LocalExists = [];
	private ZipArchive archive;
	private bool disposedValue;

	public ZipArchiveSearchPath(string rootArchive) {
		archive = new ZipArchive(new FileStream(rootArchive, FileMode.Open), ZipArchiveMode.Read, false);
	}
	public ZipArchiveSearchPath(string pathID, string path) {
		var stream = Filesystem.Open(pathID, path, FileAccess.Read, FileMode.Open);

		archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
	}

	private string FullNameOf(ZipArchiveEntry entry) => entry.FullName.Replace("\\", "/");

	public override bool CheckFile(string path, FileAccess? specificAccess, FileMode? specificMode) {
		return archive.Entries.FirstOrDefault(x => FullNameOf(x) == path) != null;
	}

	protected override bool CheckDirectory(string path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
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