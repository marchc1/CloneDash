using CloneDash.Common.Compatibility;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;

namespace CloneDash.Elfins;

[MarkForStaticConstruction]
public sealed class ElfinMod : ActiveModStore<IElfinProvider, IElfinDescriptor>
{
	public static ElfinMod Instance => ModStores.Get<ElfinMod>();

	public static ConVar elfin = new(nameof(elfin), "", FCvar.Saved | FCvar.NotInGame, "Your elfin.");

	static ElfinMod() {
		_ = Instance;
	}

	public ElfinMod() : base("Elfins", "elfin") {
		BindConVar(elfin);
	}

	[ConCommand(Help: "Your elfins info, based on the Clone Dash Descriptor")]
	public static void elfininfo(ConCommand cmd, in TokenizedCommand args) {
		var info = Instance.GetActive();
		if (info == null) {
			Logs.Print("No elfin selected.");
			return;
		}

		Logs.Print($"Elfin Info:");
		Logs.Print($"    UID:       {info.GetUniqueID()}");
		Logs.Print($"    Name:      {info.GetName()}");
		Logs.Print($"    Perk:      {info.GetPerk()}");
	}

	[ConCommand(Help: "Prints all available elfins")]
	public static void elfins(ConCommand cmd, in TokenizedCommand args) => Instance.PrintAll();
}
