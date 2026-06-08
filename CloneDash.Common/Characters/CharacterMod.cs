using CloneDash.Common;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Util;

namespace CloneDash.Characters;

[MarkForStaticConstruction]
public static class CharacterMod
{
	private static ICharacterDescriptor? activeDescriptor;
	public delegate void CharacterUpdatedDelegate(ICharacterDescriptor? charDescriptor);
	public static event CharacterUpdatedDelegate? CharacterUpdated;

	static ICharacterProvider[]? providers;

	public static ICharacterDescriptor? GetActiveCharacterDescriptor() {
		if (activeDescriptor == null)
			activeDescriptor = GetCharacterData();

		return activeDescriptor;
	}

	public static ConVar character = new(nameof(character), "character/musedash1/char_1_rock", FCvar.Saved | FCvar.NotInGame, "Your character.", null, null, (cv, o, n) => {
		var lastDescriptor = activeDescriptor;
		activeDescriptor = GetCharacterData();
		if (lastDescriptor != activeDescriptor)
			CharacterUpdated?.Invoke(activeDescriptor);
	}, autocomplete: clonedash_character_autocomplete);

	private static void clonedash_character_autocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		var availableCharacters = GetAvailableCharacters().Where(x => x.StartsWith(args.ArgS(curArgPos))).ToArray();
		returns = availableCharacters;
	}



	[ConCommand(Help: "Your characters info, based on the Clone Dash Descriptor")]
	public static void characterinfo(ConCommand cmd, in TokenizedCommand args) {
		var info = GetCharacterData();
		if (info == null) {
			Logs.Error("Info was null!");
			return;
		}

		var language = HumanLanguage.GetCurrentLanguage();

		Logs.Print($"Character Info:");
		Logs.Print($"    Name:      {info.GetName(language, out _)}");
		Logs.Print($"    Author:    {info.GetAuthor(language, out _)}");
		Logs.Print($"    Perk:      {info.GetPerk(language, out _)}");
	}
	[ConCommand(Help: "Prints all available characters")]
	public static void characters(ConCommand cmd, in TokenizedCommand args) {
		var characters = GetAvailableCharacters();
		foreach (var character in characters)
			Logs.Print($"    {character}");
	}

	static CharacterMod() {
	}

	public static IEnumerable<string> GetAvailableCharacters() {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<ICharacterProvider>();
		foreach (var retriever in providers)
			foreach (var characterName in retriever.GetAvailable())
				yield return characterName;
	}

	public static ICharacterDescriptor? GetCharacterData(string? name = null) {
		providers ??= ReflectionTools.InstantiateAllInheritorsOfInterface<ICharacterProvider>();
		name ??= character == null ? default : new(character.GetString());

		if (string.IsNullOrWhiteSpace(name))
			return null;

		foreach (var retriever in providers) {
			ICharacterDescriptor? descriptor = retriever.FindByName(name);
			if (descriptor == null) continue;

			return descriptor;
		}

		Logs.Warn($"WARNING: The character '{name}' could not be found!");
		return null;
	}
}