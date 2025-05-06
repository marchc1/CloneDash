using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Files;

namespace CloneDash.Modding.Settings
{
	[Nucleus.MarkForStaticConstruction]
	public static class FeverMod
	{
		private static FeverDescriptor? activeDescriptor;
		public delegate void UpdatedDelegate(FeverDescriptor? descriptor);
		public static event UpdatedDelegate? FeverUpdated;
		public static ConVar clonedash_fever = ConVar.Register("clonedash_fever", "default", ConsoleFlags.Saved, "Your fever effect.", null, null, (cv, o, n) => {
			activeDescriptor = null;
			activeDescriptor = GetFeverData();
			FeverUpdated?.Invoke(activeDescriptor);
		});

		static FeverMod() {
		}

		public static string[] GetAvailableFevers() {
			var dirs = Filesystem.FindDirectories("fevers", "");
			return dirs.ToArray();
		}

		public static FeverDescriptor? GetFeverData() {
			string name = clonedash_fever?.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				return null;
			}

			FeverDescriptor? descriptor = FeverDescriptor.ParseFever(Path.Combine(name, "fever.cdd"));
			if (descriptor == null) {
				Logs.Warn($"WARNING: The fever '{name}' could not be found by the file system!");
				return null;
			}

			descriptor.Filename = name;
			descriptor.MountToFilesystem();

			return descriptor;
		}
	}
}