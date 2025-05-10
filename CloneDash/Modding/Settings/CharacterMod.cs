using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Util;

namespace CloneDash.Modding.Settings
{
	[Nucleus.MarkForStaticConstruction]
	public static class CharacterMod
	{
		private static ICharacterDescriptor? activeDescriptor;
		public delegate void CharacterUpdatedDelegate(ICharacterDescriptor? charDescriptor);
		public static event CharacterUpdatedDelegate? CharacterUpdated;
		public static ConVar clonedash_character = ConVar.Register("clonedash_character", "", ConsoleFlags.Saved, "Your character.", null, null, (cv, o, n) => {
			activeDescriptor = null;
			activeDescriptor = GetCharacterData();
			CharacterUpdated?.Invoke(activeDescriptor);
		});
		public static ConCommand clonedash_characterinfo = ConCommand.Register("clonedash_characterinfo", (_, _) => {
			var info = GetCharacterData();
			if (info == null) {
				Logs.Error("Info was null!");
				return;
			}

			Logs.Print($"Character Info:");
			Logs.Print($"    Name:      {info.GetName()}");
			Logs.Print($"    Author:    {info.GetAuthor()}");
			Logs.Print($"    Perk:      {info.GetPerk()}");
		}, "Your characters info, based on the Clone Dash Descriptor");
		public static ConCommand clonedash_allcharacters = ConCommand.Register("clonedash_allcharacters", (_, _) => {
			var characters = GetAvailableCharacters();
			foreach (var character in characters)
				Logs.Print($"    {character}");
		}, "Prints all available characters");

		static CharacterMod() {
		}

		public static IEnumerable<string> GetAvailableCharacters() {
			ICharacterRetriever[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ICharacterRetriever>();
			foreach (var retriever in retrievers)
				foreach (var characterName in retriever.GetAvailableCharacters())
					yield return characterName;
		}

		public static ICharacterDescriptor? GetCharacterData() {
			ICharacterRetriever[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ICharacterRetriever>();
			string? name = clonedash_character?.GetString();

			if (string.IsNullOrWhiteSpace(name))
				return null;

			foreach (var retriever in retrievers) {
				ICharacterDescriptor? descriptor = retriever.GetDescriptorFromName(name);
				if (descriptor == null) continue;

				return descriptor;
			}

			Logs.Warn($"WARNING: The character '{name}' could not be found by the file system!");
			return null;
		}
	}
}