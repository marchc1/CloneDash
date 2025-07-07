using Nucleus;
using Nucleus.Core;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Nucleus
{
	// to-do
	public enum ConsoleFlags : ulong
	{
		None = 0,
		Saved = 1 << 0,
		DevelopmentOnly = 1 << 60 
	}
	public struct CVValue
	{
		public static CVValue Null => new();

		public string String;
		public int Length => String.Length;
		public float? AsFloat => AsDouble.HasValue ? (float)AsDouble.Value : null;
		public double? AsDouble;
		public int? AsInt;
		public static bool Update(ref CVValue cv, string input) {
			var different = cv.String != input;
			cv.String = input;
			if (double.TryParse(input, out double d)) {
				cv.AsDouble = d;
				if (int.TryParse(input, out int i))
					cv.AsInt = i;
				else
					cv.AsInt = Convert.ToInt32(Math.Round(d));
			}
			else {
				cv.AsDouble = null;
				cv.AsInt = null;
			}
			return different;
		}
		public static bool Update(ref CVValue cv, string input, double? min, double? max) {
			var updated = Update(ref cv, input);
			var clampChanged = Clamp(ref cv, min, max);
			return updated || clampChanged;
		}
		public static bool Clamp(ref CVValue cv, double? min, double? max) {
			if (cv.AsDouble == null) return false;
			var oldD = cv.AsDouble;
			var oldI = cv.AsInt;
			if (min.HasValue) cv.AsDouble = Math.Max(cv.AsDouble.Value, min.Value);
			if (max.HasValue) cv.AsDouble = Math.Min(cv.AsDouble.Value, max.Value);
			cv.AsInt = Convert.ToInt32(Math.Round(cv.AsDouble.Value));
			return cv.AsDouble != oldD || cv.AsInt != oldI;
		}
	}
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
	}
	public class ConCommandArguments
	{
		private string? __raw;
		public string Raw {
			get {
				if (__raw == null)
					return string.Join(' ', AsStringArray);

				return __raw;
			}
			set {
				__raw = value;
			}
		}
#nullable disable
		private CVValue[] __args;
#nullable enable
		public int Length => __args.Length;
		public string[] AsStringArray {
			get {
				string[] ret = new string[__args.Length];
				for (int i = 0; i < __args.Length; i++) {
					ret[i] = __args[i].String;
				}
				return ret;
			}
		}
		public static ConCommandArguments FromArray(string[] args) {
			CVValue[] cvValueArgs = new CVValue[args.Length];
			for (int i = 0; i < args.Length; i++) {
				CVValue cv = new();
				CVValue.Update(ref cv, args[i]);
				cvValueArgs[i] = cv;
			}

			return new() {
				__args = cvValueArgs
			};
		}
		public static ConCommandArguments FromString(string args, int curWritePos, out int curArgPos) {
			var arguments = new List<string>();
			curArgPos = -1;

			bool inQuotes = false;
			int argStart = -1;
			int currentArgIndex = 0;

			for (int i = 0; i <= args.Length; i++) {
				bool isEnd = i == args.Length;
				char c = !isEnd ? args[i] : '\0';

				if (!inQuotes && (isEnd || char.IsWhiteSpace(c))) {
					if (argStart != -1) {
						string arg = args.Substring(argStart, i - argStart);
						arguments.Add(arg);

						if (curWritePos >= argStart && curWritePos <= i)
							curArgPos = currentArgIndex;

						currentArgIndex++;
						argStart = -1;
					}
				}
				else if (c == '"') {
					if (inQuotes) {
						if (argStart != -1) {
							string arg = args.Substring(argStart, i - argStart);
							arguments.Add(arg);

							if (curWritePos >= argStart && curWritePos <= i)
								curArgPos = currentArgIndex;

							currentArgIndex++;
							argStart = -1;
						}
						inQuotes = false;
					}
					else {
						inQuotes = true;
						argStart = i + 1; // skip quote
					}
				}
				else if (argStart == -1) {
					argStart = i;
				}
			}

			var ret = FromArray(arguments.ToArray());
			ret.Raw = args;
			return ret;
		}
		public static ConCommandArguments FromString(string args) => FromString(args, 0, out _);
		public bool GetInt(int pos, [NotNullWhen(true)] out int? ret) {
			if (pos < 0) goto IsNull;
			if (pos >= __args.Length) goto IsNull;

			CVValue v = __args[pos];
			if (v.AsInt == null) goto IsNull;

			ret = v.AsInt;
			return true;

		IsNull:
			ret = null;
			return false;
		}
		public int? GetInt(int pos) => GetInt(pos, out int? ret) ? ret : null;

		public bool GetDouble(int pos, [NotNullWhen(true)] out double? ret) {
			if (pos < 0) goto IsNull;
			if (pos >= __args.Length) goto IsNull;

			CVValue v = __args[pos];
			if (v.AsDouble == null) goto IsNull;

			ret = v.AsDouble;
			return true;

		IsNull:
			ret = null;
			return false;
		}
		public double? GetDouble(int pos) => GetDouble(pos, out double? ret) ? ret : null;

		public bool GetString(int pos, [NotNullWhen(true)] out string? ret) {
			if (pos < 0) goto IsNull;
			if (pos >= __args.Length) goto IsNull;

			CVValue v = __args[pos];
			ret = v.String;
			return true;

		IsNull:
			ret = null;
			return false;
		}
		public string? GetString(int pos) => GetString(pos, out string? ret) ? ret : null;
	}
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
	public class ConVar : ConCommandBase
	{
		public static new ConVar Get(string name) => (ConVar)ConCommandBase.Get(name)!;
		public delegate void OnConvarChangeDelegate(ConVar self, CVValue old, CVValue now);
		private static Dictionary<string, string> __startupParms = [];
		public static void SetStartupParameter(string parameterName, string parameterStringValue) {
			ConCommandBase? cv = Get(parameterName);
			if (cv != null && cv is ConVar convar) {
				convar.SetValue(parameterStringValue);
			}
			else __startupParms[parameterName] = parameterStringValue;
		}
		public override bool IsCommand => false;
		public event ChangeCallback? OnChange;
		public string DefaultValue { get; set; }
		private double? minimum = null;
		private double? maximum = null;
		private CVValue value = new();

		public bool IsDefault => DefaultValue == value.String;

		public double? Minimum {
			get => minimum;
			set {
				minimum = value;
				Clamp();
			}
		}
		public double? Maximum {
			get => maximum;
			set {
				maximum = value;
				Clamp();
			}
		}
		public ConVar(string name, string defaultValue, ConsoleFlags flags, string helpString, AutocompleteDelegate? autocomplete = null) : base(name, helpString, flags) {
			DefaultValue = defaultValue;
			OnAutocomplete = autocomplete;
			Update(DefaultValue, true);
		}
		public ConVar(string name, string defaultValue, ConsoleFlags flags) : this(name, defaultValue, flags, "", null) { }
		public ConVar(string name, string defaultValue, ConsoleFlags flags, string helpString, double? min = null, double? max = null, AutocompleteDelegate? autocomplete = null) : this(name, defaultValue, flags, helpString, autocomplete) {
			Minimum = min;
			Maximum = max;
		}
		private void Update(string input, bool init = false) {
			var old = value;
			var changed = CVValue.Update(ref value, input, Minimum, Maximum);
			if (changed)
				OnChange?.Invoke(this, old, value);

			if (!init && (Flags & ConsoleFlags.Saved) == ConsoleFlags.Saved) {
				if (!IsDefault)
					Host.SetConfigCVar(Name, input);
				else
					Host.SetConfigCVar(Name, null);

				Host.MarkDirty();
			}
		}
		private void Clamp() {
			var old = value;
			var changed = CVValue.Clamp(ref value, Minimum, Maximum);
			if (changed)
				OnChange?.Invoke(this, old, value);
		}
		public void Revert() {
			Update(DefaultValue);
		}
		public double GetDouble() => value.AsDouble ?? 0;
		public int GetInt() => value.AsInt ?? 0;
		public string GetString() => value.String ?? "";
		public bool GetBool() => (value.AsDouble ?? 0) >= 1;
		public void SetValue(string str) => Update(str);
		public void SetValue(int i) => Update(Convert.ToString(i, CultureInfo.InvariantCulture));
		public void SetValue(double d) => Update(Convert.ToString(d, CultureInfo.InvariantCulture));
		public void SetValue(bool b) => Update(b ? "1" : "0");

		public static ConVar Register(string name, string defaultValue) => Register(name, defaultValue, ConsoleFlags.None, "");
		public static ConVar Register(string name, string defaultValue, ConsoleFlags flags) => Register(name, defaultValue, flags, "");
		public static ConVar Register(string name, double defaultValue) => Register(name, $"{defaultValue}", ConsoleFlags.None, "");
		public static ConVar Register(string name, double defaultValue, ConsoleFlags flags) => Register(name, $"{defaultValue}", flags, "");
		public static ConVar Register(string name, double defaultValue, ConsoleFlags flags, double min, double max) => Register(name, $"{defaultValue}", flags, "", min, max);
		public static ConVar Register(string name, double defaultValue, ConsoleFlags flags, string helpText, double min, double max) => Register(name, $"{defaultValue}", flags, helpText, min, max);

		public static ConVar Register(
			string name,
			string defaultValue,
			ConsoleFlags flags,
			string helpString,
			double? min = null,
			double? max = null,
			ChangeCallback? callback = null,
			bool callback_first = true,
			AutocompleteDelegate? autocomplete = null
		) {
			if (flags.HasFlag(ConsoleFlags.DevelopmentOnly) && !Debugger.IsAttached) return new(name, defaultValue, flags);

			if (IsRegistered(name)) {
				var t = lookup[name];
				if (t is ConVar cv)
					return cv;
				throw new Exception($"ConCommandBase '{name}' already existed and was not a ConVar");
			}
			ConVar? cmd = (ConVar?)Activator.CreateInstance(typeof(ConVar), [name, defaultValue, flags, helpString, min, max, autocomplete]);
			if (cmd == null) throw new Exception("ConVar: null?");
			if (__startupParms.TryGetValue(name, out string? sv)) cmd.SetValue(sv);
			if (callback != null) {
				cmd.OnChange += callback;
				if (callback_first) {
					callback(cmd, CVValue.Null, cmd.value);
				}
			}

			string? val = Host.GetConfigCVar(name);
			if (val != null)
				cmd.SetValue(val);

			lookup[name] = cmd;
			return cmd;
		}
	}

	public struct ConsoleMessage
	{
		public DateTime Time;
		public LogLevel Level;
		public string Message;

		public double Age => (DateTime.Now - Time).TotalSeconds;
		public ConsoleMessage(string message, LogLevel level = LogLevel.Info) {
			Time = DateTime.Now;
			Level = level;
			Message = message;
		}
	}
	[Nucleus.MarkForStaticConstruction]
	public static class ConsoleSystem
	{
		public static LogLevel LogLevel { get; set; } = LogLevel.Debug;
		private static List<ConsoleMessage> AllMessages = new();
		private static List<ConsoleMessage> ScreenMessages = new();

		public static ConsoleMessage[] GetMessages() => AllMessages.ToArray();
		public static int MaxConsoleMessages { get; set; } = 300;
		public static int MaxScreenMessages { get; set; } = 24;

		public static float DisappearTime { get; set; } = 0.93f;
		public static float MaxMessageTime { get; set; } = 10;
		public static void Initialize() {
			Logs.LogWrittenText += Logs_LogWrittenText;
		}
		public static void ParseSeveralCommands(string input) {
			// not implemented...
		}
		public static void ParseOneCommand(string input) {
			var whereIsSpace = input.IndexOf(' ');

			string ccname;
			string usargs;

			if (whereIsSpace == -1) {
				ccname = input;
				usargs = "";
			}
			else {
				ccname = input.Substring(0, whereIsSpace).Trim();
				usargs = input.Substring(whereIsSpace + 1).Trim();
			}

			ConCommandBase? baseC = ConCommandBase.Get(ccname);
			if (baseC == null) {
				Logs.Info($" '{ccname}' not found");
				return;
			}

			switch (baseC) {
				case ConVar cv:
					// Lets see if usargs is not set, which means a description is given
					if (usargs.Length == 0) {
						Logs.Info($"  {ccname} (default '{cv.DefaultValue}'{(cv.DefaultValue != cv.GetString() ? $", 'current {cv.GetString()})'" : ")")}");
						foreach (var line in cv.HelpString.Split("\n"))
							Logs.Info($"    {line}");
					}
					else {
						cv.SetValue(usargs);
					}
					break;
				case ConCommand cc:
					// Always run regardless of no args or not since that's how concommands work
					ConCommand.Execute(cc, usargs);
					break;
			}
		}
		private static void Logs_LogWrittenText(LogLevel level, string text) {
			ConsoleMessage message = new ConsoleMessage(text, level);
			ConsoleMessageWrittenEvent?.Invoke(ref message);

			AllMessages.Add(message);
			message.Message = message.Message.Replace("\r", "");
			ScreenMessages.Add(message);
			if (AllMessages.Count > MaxConsoleMessages)
				AllMessages.RemoveAt(0);
			if (ScreenMessages.Count > MaxScreenMessages)
				ScreenMessages.RemoveAt(0);
		}
		public delegate void ConsoleMessageWritten(ref ConsoleMessage message);
		public static event ConsoleMessageWritten? ConsoleMessageWrittenEvent;
		public static void Draw() {
			if (!EngineCore.ShowConsoleLogsInCorner || IsScreenBlockerActive)
				return;

			RenderToScreen(6, 6);
		}
		public static bool IsScreenBlockerActive => scrblockers.Count > 0;
		public static int VisibleLines => ScreenMessages.Count;
		public static int TextSize { get; set; } = 13;
		public static void RenderToScreen(int x, int y) {
			int i = 0;
			ScreenMessages.RemoveAll(x => x.Age > MaxMessageTime);

			var currentMessages = ScreenMessages.ToArray();
			foreach (ConsoleMessage message in currentMessages) {
				float fade = Math.Clamp((float)NMath.Remap(message.Age, MaxMessageTime * DisappearTime, MaxMessageTime, 1, 0), 0, 1);

				var text = $"[{Logs.LevelToConsoleString(message.Level)}] {message.Message}";
				var textSize = Graphics2D.GetTextSize(text, "Consolas", TextSize);
				Graphics2D.SetDrawColor(30, 30, 30, (int)(110 * fade));
				Graphics2D.DrawRectangle(x, y + 2 + (i * 15), textSize.W + 4, textSize.H + 4);
				Graphics2D.SetDrawColor(Logs.LevelToColor(message.Level), (int)(fade * 255));
				Graphics2D.DrawText(new(x - 1, y + 4 + (i * 15) + 1), text, "Consolas", TextSize);
				i+= 1 + text.Count(x => x == '\n');
			}
		}

		private static List<object> scrblockers = [];

		public static void AddScreenBlocker(object blocker) {
			scrblockers.Add(blocker);
		}

		public static void RemoveScreenBlocker(object blocker) {
			scrblockers.Remove(blocker);
		}

		public static void ClearScreenBlockers() {
			scrblockers.Clear();
		}
	}
}
