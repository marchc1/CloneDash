namespace Nucleus.Commands
{
	[MarkForStaticConstruction]
	public abstract class ConCommandBase
	{
		public delegate void AutocompleteDelegate(ConCommandBase cmd, string argsStr, ConCommandArguments args, int curArgPos, ref string[] returns, ref string[]? helpReturns);
		public AutocompleteDelegate? OnAutocomplete;
		protected static Dictionary<string, ConCommandBase> lookup { get; } = [];
		protected static List<ConCommandBase> __all = [];
		public static ConCommandBase[] All => __all.ToArray();
		public delegate void ChangeCallback(ConVar self, CVValue old, CVValue now);

		public string Name { get; private set; } = "";
		public string HelpString { get; private set; } = "";
		public ConsoleFlags Flags { get; private set; }
		protected ConCommandBase() {
		}
		public ConCommandBase(string name, string helpString = "", ConsoleFlags flags = ConsoleFlags.None) {
			if (IsRegistered(name))
				throw new Exception($"ConCommandBase: {name} already exists");
			Name = name;
			HelpString = helpString;
			Flags = flags;
			Register(name, this);
		}
		public static bool IsRegistered(string name) => lookup.ContainsKey(name);
		public static ConCommandBase? Get(string name) {
			if (IsRegistered(name))
				return lookup[name];
			return null;
		}

		public static ConCommandBase[] FindAllMatchesThatStartWith(string startWith) {
			List<ConCommandBase> ret = [];
			foreach (var cb in lookup) {
				if (cb.Key.StartsWith(startWith))
					ret.Add(cb.Value);
			}

			return ret.ToArray();
		}

		public static ConCommandBase[] FindAllMatchesThatContain(string needle) {
			List<ConCommandBase> ret = [];
			foreach (var cb in lookup) {
				if (cb.Key.Contains(needle))
					ret.Add(cb.Value);
			}

			ret.Sort((x, y) => x.Name.CompareTo(y.Name));

			return ret.ToArray();
		}

		public static string[] Autocomplete(ConCommandBase concmd, string argsStr, int curWritePos) {
			if (concmd.OnAutocomplete == null)
				return [];

			string[]? helpStrs = null;
#nullable disable
			string[] strs = null;
#nullable enable
			concmd.OnAutocomplete(concmd, argsStr, ConCommandArguments.FromString(argsStr, curWritePos, out int pos), pos,
#nullable disable
			ref strs,
#nullable enable
			ref helpStrs);

			if (strs == null) return [];
			return strs;
		}

		public static ConCommandBase[] FindMatchesThatStartWith(string startWith, int startAt, int max) {
			ConCommandBase[] all = FindAllMatchesThatStartWith(startWith);

			List<ConCommandBase> ret = new List<ConCommandBase>(max);
			for (int i = startAt; i < startAt + max; i++) {
				if (i >= all.Length) break;
				ret.Add(all[i]);
			}

			ret.Sort((x, y) => x.Name.CompareTo(y.Name));

			return ret.ToArray();
		}

		public static ConCommandBase[] FindMatchesThatContain(string needle, int startAt, int max) {
			ConCommandBase[] all = FindAllMatchesThatContain(needle);

			List<ConCommandBase> ret = new List<ConCommandBase>(max);
			for (int i = startAt; i < startAt + max; i++) {
				if (i >= all.Length) break;
				ret.Add(all[i]);
			}

			return ret.ToArray();
		}

		public static void Register(string name, ConCommandBase cmd) {
			if (IsRegistered(name))
				return;
			__all.Add(cmd);
			lookup.Add(name, cmd);
		}
		public abstract bool IsCommand { get; }
		public virtual bool IsFlagSet(ConsoleFlags flag) => (flag & Flags) == flag;
		public virtual void AddFlags(ConsoleFlags flags) => Flags = Flags | flags;
		public virtual void RemoveFlags(ConsoleFlags flags) => Flags = Flags & ~flags;

		[ConCommand(Help: "Lists all available convars/concommands")]
		static void cvarlist() {
			int maxWidth = 0;
			var all = __all.Where(x => !x.IsFlagSet(ConsoleFlags.Unregistered));
			foreach (var ccbase in all) if (ccbase.Name.Length > maxWidth) maxWidth = ccbase.Name.Length;
			int ccmds = 0, cvars = 0;
			foreach (var ccbase in all.OrderBy(x => x.Name)) {
				Logs.Print($"{ccbase.Name.PadRight(maxWidth, ' ')}: {ccbase.HelpString}");
				if (ccbase.IsCommand)
					ccmds++;
				else
					cvars++;
			}
			Logs.Print($"{ccmds + cvars} registered, {ccmds} commands, {cvars} vars.");
		}
		[ConCommand(Help: "Find a convar/concommand by name")]
		static void find(ConCommand cmd, ConCommandArguments args) {
			var found = __all.Where(x => !x.IsFlagSet(ConsoleFlags.Unregistered) && (x.Name.Contains(args.Raw, StringComparison.InvariantCultureIgnoreCase) || x.HelpString.Contains(args.Raw, StringComparison.InvariantCultureIgnoreCase)));
			int maxWidth = 0;
			foreach (var cvar in found) if (cvar.Name.Length > maxWidth) maxWidth = cvar.Name.Length;
			foreach (var cvar in found.OrderBy(x => x.Name)) {
				Logs.Print($"{cvar.Name.PadRight(maxWidth, ' ')}: {cvar.HelpString}");
			}
		}
	}
}
