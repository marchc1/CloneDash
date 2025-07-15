using Nucleus;
using Nucleus.Commands;
using Nucleus.Files;
using Nucleus.Util;

namespace CloneDash.Fevers
{
	[MarkForStaticConstruction]
	public static class FeverMod
	{
		private static IFeverDescriptor? activeDescriptor;
		public delegate void UpdatedDelegate(IFeverDescriptor? descriptor);
		public static event UpdatedDelegate? FeverUpdated;
		public static ConVar fever = ConVar.Register(nameof(fever), "default", ConsoleFlags.Saved, "Your fever effect.", null, null, (cv, o, n) => {
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

		public static IFeverDescriptor? GetFeverData() {
			string? name = fever?.GetString();
			if (string.IsNullOrWhiteSpace(name))
				return null;

			IFeverProvider[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<IFeverProvider>();
			foreach (var retriever in retrievers) {
				IFeverDescriptor? descriptor = retriever.FindByName(name);
				if (descriptor == null) continue;

				return descriptor;
			}

			return null;
		}
	}
}