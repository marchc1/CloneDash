using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Files;

namespace CloneDash.Modding.Settings
{
	[Nucleus.MarkForStaticConstruction]
	public static class CharacterMod
	{
		private static CharacterDescriptor? activeDescriptor;
		public delegate void CharacterUpdatedDelegate(CharacterDescriptor? charDescriptor);
		public static event CharacterUpdatedDelegate? CharacterUpdated;
		public static ConVar clonedash_character = ConVar.Register("clonedash_character", "", ConsoleFlags.Saved, "Your character.", null, null, (cv, o, n) => {
			activeDescriptor = null;
			activeDescriptor = GetCharacterData();
			CharacterUpdated?.Invoke(activeDescriptor);
		});
		public static ConCommand clonedash_characterinfo = ConCommand.Register("clonedash_characterinfo", (_, _) => {
			var info = GetCharacterData();
			Logs.Print($"Character Info:");
			Logs.Print($"    Name:      {info.Name}");
			Logs.Print($"    Author:    {info.Author}");
			Logs.Print($"    Perk:      {info.Perk}");
		}, "Your characters info, based on the Clone Dash Descriptor");
		public static ConCommand clonedash_allcharacters = ConCommand.Register("clonedash_allcharacters", (_, _) => {
			var characters = GetAvailableCharacters();
			foreach (var character in characters)
				Logs.Print($"    {character}");
		}, "Prints all available characters");

		static CharacterMod() {
		}

		public static string[] GetAvailableCharacters() {
			var dirs = Filesystem.FindDirectories("chars", "");
			return dirs.ToArray();
		}
		public static ICharacterDescriptor? GetCharacterData() {
			string name = clonedash_character?.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				return null;
				//throw new Exception("Cannot load character; clonedash_character convar empty");
			}

			CharacterDescriptor? descriptor = CharacterDescriptor.ParseCharacter(Path.Combine(name, "character.cdd"));
			if(descriptor == null) {
				Logs.Warn($"WARNING: The character '{name}' could not be found by the file system!");
				return null;
			}

			descriptor.Filename = name;
			descriptor.MountToFilesystem();

			return descriptor;
		}
	}
}