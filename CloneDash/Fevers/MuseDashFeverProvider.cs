using Nucleus;
using Nucleus.Files;

namespace CloneDash.Fevers;

public class MuseDashFeverProvider : IFeverProvider
{
	int IFeverProvider.Priority => 10000000;

	IEnumerable<string> IFeverProvider.GetAvailable() {
		var dirs = filesystem.FindDirectories("scenes", "");
		return dirs;
	}

	IFeverDescriptor? IFeverProvider.FindByName(ReadOnlySpan<char> name) {
		MuseDashFever? descriptor = MuseDashFever.GetFever(name);
		if (descriptor == null) 
			return null;

		return descriptor;
	}
}
