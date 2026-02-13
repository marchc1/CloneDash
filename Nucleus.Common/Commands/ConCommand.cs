using Nucleus.Common.Commands;
using System.Diagnostics;

namespace Nucleus.Commands;

public delegate void CommandExecutedDelegate(ConCommand cmd, in TokenizedCommand args);

public class ConCommand : ConCommandBase
{
	public override bool IsCommand() => true;

	public void Dispatch(in TokenizedCommand command) {
		if (OnExecuted != null)
			OnExecuted(this, command);
		else
			Debug.Assert(false, $"Encountered ConCommand \"{Name}\" without a callback!");
	}

	public CommandExecutedDelegate? OnExecuted;

	public ConCommand (string name, CommandExecutedDelegate executed)  : this(name, executed, null, 0, "") { }
	public ConCommand (string name, CommandExecutedDelegate executed, AutocompleteDelegate autocomplete) : this(name, executed, autocomplete, 0, "") { }
	public ConCommand (string name, CommandExecutedDelegate executed, string helpString) : this(name, executed, null, 0, helpString) { }
	public ConCommand (string name, CommandExecutedDelegate executed, AutocompleteDelegate autocomplete, string helpString) : this(name, executed, autocomplete, 0, helpString) { }
	public ConCommand (string name, CommandExecutedDelegate executed, FCvar flags, string helpString) : this(name, executed, null, flags, helpString) { }
	public ConCommand(string name, CommandExecutedDelegate executed, AutocompleteDelegate? autocomplete, FCvar flags, string helpString) : base(name, helpString, flags) {
		OnExecuted = executed;
		OnAutocomplete = autocomplete;
	}
}
