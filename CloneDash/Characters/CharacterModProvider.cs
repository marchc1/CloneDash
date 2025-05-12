using CloneDash.Modding.Descriptors;
using Nucleus.Files;

namespace CloneDash.Characters
{
	public class CharacterModProvider : ICharacterProvider
	{
		int ICharacterProvider.Priority => 10000000;

		IEnumerable<string> ICharacterProvider.GetAvailable() {
			var dirs = Filesystem.FindDirectories("chars", "");
			return dirs;
		}

		ICharacterDescriptor? ICharacterProvider.FindByName(string name) {
			var descriptor = CD_CharacterDescriptor.ParseCharacter(Path.Combine(name, "character.cdd"));
			if (descriptor == null) return null;

			descriptor.Filename = name;
			descriptor.MountToFilesystem();
			return descriptor;
		}
	}
}