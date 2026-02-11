using System.Diagnostics;

namespace Nucleus.Commands
{
	public class ConCommand : ConCommandBase
	{
		public override bool IsCommand => true;
		public delegate void ExecutedDelegate(ConCommand cmd, ConCommandArguments args);

		public ExecutedDelegate? OnExecuted;

		public ConCommand(string name, ExecutedDelegate executed, AutocompleteDelegate? autocomplete, ConsoleFlags flags, string helpString) : base(name, helpString, flags) {
			OnExecuted = executed;
			OnAutocomplete = autocomplete;
		}

		public static ConCommand Register(string name, ExecutedDelegate executed) => Register(name, executed, ConsoleFlags.None, "");
		public static ConCommand Register(string name, ExecutedDelegate executed, AutocompleteDelegate autocomplete) => Register(name, executed, autocomplete, ConsoleFlags.None, "");
		public static ConCommand Register(string name, ExecutedDelegate executed, string helpString) => Register(name, executed, ConsoleFlags.None, helpString);
		public static ConCommand Register(string name, ExecutedDelegate executed, AutocompleteDelegate autocomplete, string helpString) => Register(name, executed, autocomplete, ConsoleFlags.None, helpString);
		public static ConCommand Register(string name, ExecutedDelegate executed, ConsoleFlags flags, string helpString) => Register(name, executed, null, flags, helpString);
		public static ConCommand Register(
			string name,
			ExecutedDelegate executed,
			AutocompleteDelegate? autocomplete,
			ConsoleFlags flags,
			string helpString
		) {
			if (flags.HasFlag(ConsoleFlags.DevelopmentOnly) && !Debugger.IsAttached) return new(name, (_, _) => { }, null, flags, "");

			if (IsRegistered(name)) {
				var t = lookup[name];
				if (t is ConCommand cc)
					return cc;
				throw new Exception($"ConCommandBase '{name}' already existed and was not a ConCommand");
			}
			ConCommand? cmd = (ConCommand?)Activator.CreateInstance(typeof(ConCommand), [name, executed, autocomplete, flags, helpString]);
			if (cmd == null) throw new Exception("ConVar: null?");
			lookup[name] = cmd;
			return cmd;
		}

		public static void Execute(ConCommand concmd, params string[] args) {
			if (concmd.OnExecuted == null)
				return;

			concmd.OnExecuted(concmd, ConCommandArguments.FromArray(args));
		}
		public static void Execute(ConCommand concmd, string args) {
			if (concmd.OnExecuted == null)
				return;

			concmd.OnExecuted(concmd, ConCommandArguments.FromString(args));
		}

		public static void Execute(string concmd, params string[] args) {
			ConCommandBase? b = Get(concmd);

			if (b == null) {
				Logs.Info($"] '{concmd}' not found");
				return;
			}

			if (b is ConCommand cc) Execute(cc, args);
			else Logs.Warn($"] '{concmd}' not a concommand");
		}
	}
}
