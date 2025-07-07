using CloneDash.Compatibility.MuseDash;

using Nucleus;
using Nucleus.Util;

namespace CloneDash.Characters;

[MarkForStaticConstruction]
public static class CharacterMod
{
	private static ICharacterDescriptor? activeDescriptor;
	public delegate void CharacterUpdatedDelegate(ICharacterDescriptor? charDescriptor);
	public static event CharacterUpdatedDelegate? CharacterUpdated;
	public static ConVar character = ConVar.Register(nameof(character), "", ConsoleFlags.Saved, "Your character.", null, null, (cv, o, n) => {
		activeDescriptor = null;
		activeDescriptor = GetCharacterData();
		CharacterUpdated?.Invoke(activeDescriptor);
	}, autocomplete: clonedash_character_autocomplete);
	private static void clonedash_character_autocomplete(ConCommandBase cmd, string argsStr, ConCommandArguments args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		var availableCharacters = CharacterMod.GetAvailableCharacters().Where(x => x.StartsWith(args.GetString(curArgPos) ?? "")).ToArray();
		returns = availableCharacters;
	}
	public static ConCommand characterinfo = ConCommand.Register(nameof(characterinfo), (_, _) => {
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
	public static ConCommand characters = ConCommand.Register(nameof(characters), (_, _) => {
		var characters = GetAvailableCharacters();
		foreach (var character in characters)
			Logs.Print($"    {character}");
	}, "Prints all available characters");

	static CharacterMod() {
	}

	public static IEnumerable<string> GetAvailableCharacters() {
		ICharacterProvider[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ICharacterProvider>();
		foreach (var retriever in retrievers)
			foreach (var characterName in retriever.GetAvailable())
				yield return characterName;
	}

	public static ICharacterDescriptor? GetCharacterData() {
		ICharacterProvider[] retrievers = ReflectionTools.InstantiateAllInheritorsOfInterface<ICharacterProvider>();
		string? name = character?.GetString();

		if (string.IsNullOrWhiteSpace(name))
			return null;

		foreach (var retriever in retrievers) {
			ICharacterDescriptor? descriptor = retriever.FindByName(name);
			if (descriptor == null) continue;

			return descriptor;
		}

		Logs.Warn($"WARNING: The character '{name}' could not be found!");
		return null;
	}
}