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
			Logs.Print("Character Info:");
			Logs.Print("    ");
		}, "Your characters info, based on the Clone Dash Descriptor");

		static CharacterMod() {
			Filesystem.AddPath("chars", Filesystem.Resolve("game") + "assets/chars/");
		}

		public static (string Name, string Filepath, CharacterDescriptor Descriptor) GetCharacterData() {
			string name = clonedash_character.GetString();
			if (string.IsNullOrWhiteSpace(name)) {
				throw new Exception("Cannot load character; clonedash_character convar empty");
			}

			CharacterDescriptor descriptor = CharacterDescriptor.ParseFile(Filesystem.Resolve($"{name}.cdd", "chars"));
			return (name, Filesystem.Resolve($"{name}.cdd", "chars"), descriptor);
		}
	}
}

