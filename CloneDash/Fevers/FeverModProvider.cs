using Nucleus;
using Nucleus.Files;

namespace CloneDash.Fevers;

public class FeverModProvider : IFeverProvider
{
	int IFeverProvider.Priority => 10000000;

	IEnumerable<string> IFeverProvider.GetAvailable() {
		var dirs = Filesystem.FindDirectories("scenes", "");
		return dirs;
	}

	IFeverDescriptor? IFeverProvider.FindByName(string name) {
		CloneDashFever? descriptor = CloneDashFever.ParseFever(Path.Combine(name, "fever.cdd"));
		if (descriptor == null) {
			Logs.Warn($"WARNING: The fever '{name}' could not be found!");
			return null;
		}

		descriptor.Filename = name;
		descriptor.MountToFilesystem();

		return descriptor;
	}
}
