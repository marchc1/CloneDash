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
			Filesystem.AddPath("chars", Filesystem.Resolve("game") + "assets/chars/");
		}

		public static string[] GetAvailableCharacters() {
			var files = Filesystem.FindDirectories("chars", "", absolutePaths: false);
			return files.ToArray();
		}
		public static CharacterDescriptor[] GetAvailableCharacterDescriptors() {
			var files = Filesystem.FindDirectories("chars", "").ToArray();
			var descriptors = new CharacterDescriptor[files.Length];
			for (int i = 0; i < files.Length; i++) {
				descriptors[i] = CharacterDescriptor.ParseFile(Path.Combine(files[i], "character.cdd"));
			}
			return descriptors;
		}

		public static CharacterDescriptor? GetCharacterData() {
			string name = clonedash_character.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				return null;
				//throw new Exception("Cannot load character; clonedash_character convar empty");
			}

			var path = Filesystem.Resolve($"{name}/character.cdd", "chars", false);
			if(path == null || !File.Exists(path)) {
				Logs.Warn($"WARNING: Bad character name '{name}'! Refusing to load CharacterDescriptor!");
				return null;
			}
			CharacterDescriptor descriptor = CharacterDescriptor.ParseFile(path);
			descriptor.Filepath = path;
			return descriptor;
		}
	}
}

