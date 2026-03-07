using Nucleus.Common.Commands;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Commands;

public delegate void ChangeCallback(ConVar self, scoped ReadOnlySpan<char> oldStr, double oldDouble);
public delegate void AutocompleteDelegate(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? helpReturns);

public ref struct ConCommandBaseSearch
{
	private ConCommandBase? next;
	public ConCommandBase? Current { get; private set; }
	public Predicate<ConCommandBase> Predicate;
	public ConCommandBaseSearch(ConCommandBase? head, Predicate<ConCommandBase> predicate) {
		next = head;
		Current = null;
		Predicate = predicate;
	}
	public bool Iterate([NotNullWhen(true)] out ConCommandBase? cc) {
		if (!MoveNext()) {
			cc = null;
			return false;
		}
		cc = Current;
		return cc != null;
	}
	public bool MoveNext() {
		while (next != null) {
			ConCommandBase candidate = next;
			next = candidate.Next;
			if (Predicate(candidate)) {
				Current = candidate;
				return true;
			}
		}
		Current = null;
		return false;
	}
}

[MarkForStaticConstruction]
public abstract class ConCommandBase : IConCommandBase
{
	public AutocompleteDelegate? OnAutocomplete;

	public static ConCommandBase? Head;
	public ConCommandBase? Next;

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

	protected virtual void CheckFlagChange(FCvar prev, FCvar now) { }

	public virtual void AddFlags(FCvar flags) {
		FCvar prev = Flags;
		Flags = Flags | flags;
		CheckFlagChange(prev, Flags);
	}

	public virtual void RemoveFlags(FCvar flags) {
		FCvar prev = Flags;
		Flags = Flags & ~flags;
		CheckFlagChange(prev, Flags);
	}




	[ConCommand(Help: "Lists all available convars/concommands")]
	static void cvarlist() {
		int maxWidth = 0;
		int ccmds = 0, cvars = 0;
		ConCommandBaseSearch searcher;

		searcher = new(cvar.GetCommands(), static x => !x.IsFlagSet(FCvar.Unregistered));
		while (searcher.Iterate(out ConCommandBase? cc))
			if (cc.Name.Length > maxWidth)
				maxWidth = cc.Name.Length;

		searcher = new(cvar.GetCommands(), static x => !x.IsFlagSet(FCvar.Unregistered));
		while (searcher.Iterate(out ConCommandBase? cc)) {
			Logs.Print($"{cc.Name.PadRight(maxWidth, ' ')}: {cc.HelpString}");
			if (cc.IsCommand())
				ccmds++;
			else
				cvars++;
		}

		Logs.Print($"{ccmds + cvars} registered, {ccmds} commands, {cvars} vars.");
	}
	[ConCommand(Help: "Find a convar/concommand by name")]
	static void find(ConCommand cmd, in TokenizedCommand args) {
		string search = new(args.ArgS());
		if (search.Length == 0) {
			Logs.Print("Usage: find <string>");
			return;
		}

		int maxWidth = 0;
		ConCommandBaseSearch searcher;
		searcher = new(cvar.GetCommands(), x => !x.IsFlagSet(FCvar.Unregistered)
			&& (x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
			 || x.HelpString.Contains(search, StringComparison.OrdinalIgnoreCase)));

		while (searcher.Iterate(out ConCommandBase? cc))
			if (cc.Name.Length > maxWidth)
				maxWidth = cc.Name.Length;

		searcher = new(cvar.GetCommands(), x => !x.IsFlagSet(FCvar.Unregistered)
			&& (x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
			 || x.HelpString.Contains(search, StringComparison.OrdinalIgnoreCase)));

		int count = 0;
		while (searcher.Iterate(out ConCommandBase? cc)) {
			Logs.Print($"{cc.Name.PadRight(maxWidth, ' ')}: {cc.HelpString}");
			count++;
		}
		Logs.Print($"{count} result{(count != 1 ? "s" : "")} for \"{search}\".");
	}

	public virtual void Init() {
		cvar.RegisterConCommand(this);
	}
}
