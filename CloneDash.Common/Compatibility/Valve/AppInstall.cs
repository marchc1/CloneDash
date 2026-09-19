
#if COMPILED_WINDOWS
using System.Diagnostics.CodeAnalysis;
#endif

namespace CloneDash.Common.Compatibility.Valve;

public readonly struct AppInstall
{
	public readonly AppStatus Status;
	public readonly string? Path;

	public AppInstall(AppStatus status) {
		Status = status;
	}

	public AppInstall(string path) {
		Status = AppStatus.OK;
		Path = path;
	}

	[MemberNotNullWhen(true, nameof(Path))]
	public readonly bool IsOK([NotNullWhen(true)] out string? path, out AppStatus status) {
		status = Status;
		if (Path == null || Status != AppStatus.OK) {
			path = null;
			return false;
		}
		path = Path;
		return true;
	}
}
