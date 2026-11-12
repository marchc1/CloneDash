using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Common.Input;
using Nucleus.Core;

namespace Nucleus.Engine;

public readonly record struct ActionBinding(ButtonCode Button, int Action);

public class KeyBindSet
{
	readonly string?[] commands = new string?[Key.BUTTON_COUNT];
	readonly string?[] defaults = new string?[Key.BUTTON_COUNT];

	internal KeyBindSet(string name) => Name = name;

	public ActionBinding[] Actions { get; private set; } = [];
	public bool Configured { get; internal set; }
	public string Name { get; }

	public IEnumerable<ButtonCode> ButtonsOf(int action) {
		foreach (var binding in Actions)
			if (binding.Action == action)
				yield return binding.Button;
	}

	public string? GetCommand(ButtonCode button) => Key.IsBindable(button) ? commands[(int)button] : null;

	public IEnumerable<(ButtonCode Button, string Command)> GetCommands() {
		for (int i = 0; i < commands.Length; i++)
			if (commands[i] is string command)
				yield return ((ButtonCode)i, command);
	}

	public void ResetToDefaults() {
		defaults.CopyTo(commands, 0);
		Resolve();
		Host.TriggerResave();
	}

	internal void Clear() {
		Array.Clear(commands);
		Resolve();
	}

	internal void Resolve() {
		List<ActionBinding> actions = [];
		for (int i = 0; i < commands.Length; i++)
			if (commands[i] is string command && command.Length > 1 && command[0] == '+' && Key.FindAction(command.AsSpan(1)) is int action and >= 0)
				actions.Add(new((ButtonCode)i, action));
		Actions = actions.ToArray();
	}

	internal void SetCommand(ButtonCode button, string? command) {
		commands[(int)button] = command;
		Resolve();
	}

	internal void SetDefault(ButtonCode button, string command) {
		defaults[(int)button] = command;
		if (!Configured)
			SetCommand(button, command);
	}
}

public static class Key
{
	public const int BUTTON_COUNT = (int)ButtonCode.MouseLast + 1;
	public const string COMMON = "common";

	static readonly List<string> actionNames = [];
	static readonly Dictionary<string, int> actions = new(StringComparer.OrdinalIgnoreCase);
	static readonly Dictionary<string, ButtonCode> buttonsByName = BuildButtonNames();
	static readonly Dictionary<string, KeyBindSet> sets = new(StringComparer.OrdinalIgnoreCase);

	public static int ActionCount => actionNames.Count;
	public static IEnumerable<KeyBindSet> Sets => sets.Values;

	[ConCommand(Name: "bind", Help: "bind <key> [command] [set]: binds a key to a command, or prints the current binding")]
	public static void BindCommand(in TokenizedCommand args) {
		if (args.ArgC() < 2) {
			Logs.Print("bind <key> [command] [set]");
			return;
		}
		if (!TryParseButton(args[1], out var button)) {
			Logs.Print($"\"{args[1]}\" isn't a valid key");
			return;
		}

		string set = args.ArgC() > 3 ? new string(args[3]) : COMMON;
		if (args.ArgC() == 2) {
			string? command = GetSet(set).GetCommand(button);
			Logs.Print(command == null ? $"\"{button.GetString()}\" is not bound in {set}" : $"\"{button.GetString()}\" = \"{command}\" in {set}");
			return;
		}

		Bind(set, button, new string(args[2]));
	}

	[ConCommand(Name: "key_listboundkeys", Help: "key_listboundkeys [set]: lists every bound key")]
	public static void ListBoundKeys(in TokenizedCommand args) {
		string? only = args.ArgC() > 1 ? new string(args[1]) : null;
		foreach (var set in sets.Values.OrderBy(set => set.Name))
			if (only == null || set.Name.Equals(only, StringComparison.OrdinalIgnoreCase))
				foreach (var (button, command) in set.GetCommands())
					Logs.Print($"[{set.Name}] \"{button.GetString()}\" = \"{command}\"");
	}

	[ConCommand(Name: "unbind", Help: "unbind <key> [set]: removes a key's binding")]
	public static void UnbindCommand(in TokenizedCommand args) {
		if (args.ArgC() < 2) {
			Logs.Print("unbind <key> [set]");
			return;
		}
		if (!TryParseButton(args[1], out var button)) {
			Logs.Print($"\"{args[1]}\" isn't a valid key");
			return;
		}

		Unbind(args.ArgC() > 2 ? new string(args[2]) : COMMON, button);
	}

	[ConCommand(Name: "unbindall", Help: "unbindall [set]: removes every binding, or every binding in one set")]
	public static void UnbindAllCommand(in TokenizedCommand args) {
		if (args.ArgC() > 1)
			UnbindAll(new string(args[1]));
		else
			foreach (var set in sets.Values)
				UnbindAll(set.Name);
	}

	public static void Bind(string set, ButtonCode button, string command) {
		if (!IsBindable(button))
			return;

		var bindSet = GetSet(set);
		bindSet.Configured = true;
		bindSet.SetCommand(button, command);
		Host.TriggerResave();
	}

	public static int FindAction(ReadOnlySpan<char> name) => actions.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out int action) ? action : -1;

	public static string GetActionName(int action) => actionNames[action];

	public static KeyBindSet GetSet(string name) {
		if (!sets.TryGetValue(name, out var set)) {
			set = new KeyBindSet(name);
			sets[name] = set;
		}
		return set;
	}

	public static bool IsBindable(ButtonCode button) => button > ButtonCode.None && (int)button < BUTTON_COUNT;

	public static int RegisterAction(string name) {
		if (actions.TryGetValue(name, out int action))
			return action;

		action = actionNames.Count;
		actionNames.Add(name);
		actions[name] = action;
		foreach (var set in sets.Values)
			set.Resolve();
		return action;
	}

	public static void RegisterDefault(string set, ButtonCode button, string command) {
		if (IsBindable(button))
			GetSet(set).SetDefault(button, command);
	}

	public static bool TryParseButton(ReadOnlySpan<char> name, out ButtonCode button)
		=> buttonsByName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out button);

	public static void Unbind(string set, ButtonCode button) {
		if (!IsBindable(button))
			return;

		var bindSet = GetSet(set);
		bindSet.Configured = true;
		bindSet.SetCommand(button, null);
		Host.TriggerResave();
	}

	public static void UnbindAll(string set) {
		var bindSet = GetSet(set);
		bindSet.Configured = true;
		bindSet.Clear();
		Host.TriggerResave();
	}

	public static void WriteBindings(StreamWriter writer) {
		foreach (var set in sets.Values.OrderBy(set => set.Name)) {
			writer.WriteLine($"unbindall \"{set.Name}\"");
			foreach (var (button, command) in set.GetCommands())
				writer.WriteLine($"bind \"{button.GetString()}\" \"{command}\" \"{set.Name}\"");
		}
	}

	static Dictionary<string, ButtonCode> BuildButtonNames() {
		Dictionary<string, ButtonCode> names = new(StringComparer.OrdinalIgnoreCase);
		for (int i = (int)ButtonCode.None + 1; i < BUTTON_COUNT; i++) {
			var button = (ButtonCode)i;
			string name = button.GetString();
			if (name != "Unknown")
				names.TryAdd(name, button);
		}
		return names;
	}
}