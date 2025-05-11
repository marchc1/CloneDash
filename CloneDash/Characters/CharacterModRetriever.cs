using CloneDash.Modding.Descriptors;
using Nucleus.Files;

namespace CloneDash.Characters
{
	public class CharacterModRetriever : ICharacterRetriever
	{
		int ICharacterRetriever.Priority => 10000000;

		IEnumerable<string> ICharacterRetriever.GetAvailableCharacters() {
			var dirs = Filesystem.FindDirectories("chars", "");
			return dirs;
		}

		ICharacterDescriptor? ICharacterRetriever.GetDescriptorFromName(string name) {
			var descriptor = CharacterDescriptor.ParseCharacter(Path.Combine(name, "character.cdd"));
			if (descriptor == null) return null;

			descriptor.Filename = name;
			descriptor.MountToFilesystem();
			return descriptor;
		}
	}
}