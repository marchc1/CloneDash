namespace Nucleus.Files;

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

		if (specificMode.HasValue && specificMode.Value == FileMode.Open && !info.Exists) return false;
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
