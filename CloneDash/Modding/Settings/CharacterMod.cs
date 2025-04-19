using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CloneDash.Modding.Settings
{
	[Nucleus.MarkForStaticConstruction]
	public static class CharacterMod
	{
		public static ConVar clonedash_character = ConVar.Register("clonedash_character", "", ConsoleFlags.Saved, "Your character.");
		public static ConCommand clonedash_characterinfo = ConCommand.Register("clonedash_characterinfo", (_, _) => {
			var info = GetCharacterData();
			Logs.Print($"Character Info:");
			Logs.Print($"    Name:      {info.Name}");
			Logs.Print($"    Author:    {info.Author}");
			Logs.Print($"    Perk:      {info.Perk}");
			Logs.Print($"    Version:   {info.Version}");
			Logs.Print($"    Max HP:    {info.MaxHP}");
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
		public static CharacterDescriptor[] GetAvailableCharacterDescriptors() {
			var dirs = Filesystem.FindDirectories("chars", "").ToArray();
			var descriptors = new CharacterDescriptor?[dirs.Length];

			for (int i = 0; i < dirs.Length; i++) 
				descriptors[i] = CharacterDescriptor.ParseCharacter(Path.Combine(dirs[i], "character.cdd"));
			
			var notNull = 0;
			for (int i = 0; i < dirs.Length; i++) 
				if (descriptors[i] != null) notNull++;

			var notNullReturn = new CharacterDescriptor[notNull];
			var notNullPtr = 0;
			for (int i = 0; i < dirs.Length; i++) {
				var descriptor = descriptors[i];
				if (descriptor != null) {
					notNullReturn[notNullPtr] = descriptor;
					notNullPtr++;
				}
			}
			
			return notNullReturn;
		}

		public static CharacterDescriptor? GetCharacterData() {
			string name = clonedash_character.GetString();
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

