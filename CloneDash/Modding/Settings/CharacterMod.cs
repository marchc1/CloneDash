using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		static CharacterMod() {
			Filesystem.AddPath("chars", Filesystem.Resolve("game") + "assets/chars/");
		}

		public static CharacterDescriptor? GetCharacterData() {
			string name = clonedash_character.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				return null;
				//throw new Exception("Cannot load character; clonedash_character convar empty");
			}

			var path = Filesystem.Resolve($"{name}.cdd", "chars");
			CharacterDescriptor descriptor = CharacterDescriptor.ParseFile(path);
			descriptor.Filepath = path;
			return descriptor;
		}
	}
}

