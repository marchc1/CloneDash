using CloneDash.Common.Compatibility;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;

namespace CloneDash.Characters;

[MarkForStaticConstruction]
public sealed class CharacterMod : ActiveModStore<ICharacterProvider, ICharacterDescriptor>
{
	public static CharacterMod Instance => ModStores.Get<CharacterMod>();

	public static ConVar character = new(nameof(character), "character/musedash1/char_1_rock", FCvar.Saved | FCvar.NotInGame, "Your character.");

	static CharacterMod() {
		_ = Instance;
	}

	public CharacterMod() : base("Characters", "character") {
		BindConVar(character);
	}

	[ConCommand(Help: "Your characters info, based on the Clone Dash Descriptor")]
	public static void characterinfo(ConCommand cmd, in TokenizedCommand args) {
		var info = Instance.GetActive();
		if (info == null) {
			Logs.Error("Info was null!");
			return;
		}

		Logs.Print($"Character Info:");
		Logs.Print($"    UID:       {info.GetUniqueID()}");
		Logs.Print($"    Name:      {info.GetName()}");
		Logs.Print($"    Author:    {info.GetAuthor()}");
		Logs.Print($"    Perk:      {info.GetPerk()}");
	}

	[ConCommand(Help: "Prints all available characters")]
	public static void characters(ConCommand cmd, in TokenizedCommand args) => Instance.PrintAll();
}
