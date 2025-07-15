using Nucleus.Core;

using System.Diagnostics;
using System.Globalization;

namespace Nucleus.Commands
{
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
}
