using Nucleus.Common.Commands;

namespace Nucleus.Commands;

public delegate void ChangeCallback(ConVar self, scoped ReadOnlySpan<char> oldStr, double oldDouble);
public delegate void AutocompleteDelegate(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? helpReturns);

[MarkForStaticConstruction]
public abstract class ConCommandBase : IConCommandBase
{
	public static ConCommandBase? Head;
	public ConCommandBase? Next;

	public AutocompleteDelegate? OnAutocomplete;

	public bool Registered;
	public bool IsRegistered() => Registered;

	public virtual bool IsCommand() => false;
	public string Name = "";
	public string HelpString = "";
	public FCvar Flags;

	public ReadOnlySpan<char> GetName() => Name;
	public ReadOnlySpan<char> GetHelpText() => HelpString;
	public FCvar GetFlags() => Flags;

	static readonly object linkedListLock = new();

	protected ConCommandBase() {
		lock (linkedListLock) {
			Next = Head;
			Head = this;
		}
	}
	public ConCommandBase(string name, string helpString = "", FCvar flags = FCvar.None) : this() {
		Name = name;
		HelpString = helpString;
		Flags = flags;
	}

	public virtual bool IsFlagSet(FCvar flag) => (flag & Flags) == flag;
	public virtual void AddFlags(FCvar flags) => Flags = Flags | flags;
	public virtual void RemoveFlags(FCvar flags) => Flags = Flags & ~flags;




	[ConCommand(Help: "Lists all available convars/concommands")]
	static void cvarlist() {
		//int maxWidth = 0;
		//var all = __all.Where(x => !x.IsFlagSet(ConsoleFlags.Unregistered));
		//foreach (var ccbase in all) if (ccbase.Name.Length > maxWidth) maxWidth = ccbase.Name.Length;
		//int ccmds = 0, cvars = 0;
		//foreach (var ccbase in all.OrderBy(x => x.Name)) {
		//	Logs.Print($"{ccbase.Name.PadRight(maxWidth, ' ')}: {ccbase.HelpString}");
		//	if (ccbase.IsCommand)
		//		ccmds++;
		//	else
		//		cvars++;
		//}
		//Logs.Print($"{ccmds + cvars} registered, {ccmds} commands, {cvars} vars.");
	}
	[ConCommand(Help: "Find a convar/concommand by name")]
	static void find(ConCommand cmd, in TokenizedCommand args) {
		//	var found = __all.Where(x => !x.IsFlagSet(ConsoleFlags.Unregistered) && (x.Name.Contains(args.Raw, StringComparison.InvariantCultureIgnoreCase) || x.HelpString.Contains(args.Raw, StringComparison.InvariantCultureIgnoreCase)));
		//	int maxWidth = 0;
		//	foreach (var cvar in found) if (cvar.Name.Length > maxWidth) maxWidth = cvar.Name.Length;
		//	foreach (var cvar in found.OrderBy(x => x.Name)) {
		//		Logs.Print($"{cvar.Name.PadRight(maxWidth, ' ')}: {cvar.HelpString}");
		//	}
	}

	public virtual void Init() {
		cvar.RegisterConCommand(this);
	}
}
