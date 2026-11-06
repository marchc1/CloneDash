using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Input;
using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Engine;

namespace CloneDash.Settings;

public record class KeyBinding
{
	public ButtonCode Key;
	public int Action;

	public KeyBinding() { }
	public KeyBinding(ButtonCode key, int action) {
		Key = key;
		Action = action;
	}
}
public record class MouseBinding
{
	public ButtonCode Button;
	public int Action;

	public MouseBinding(ButtonCode btn, int action) {
		Button = btn;
		Action = action;
	}
}

// Sucks, but we made mistakes in the serialized data, and this is the cleanest way to handle those mistakes.
public class InputDataStore_SerializedBeforeFeb9th2026
{
	public Dictionary<int, int> KeyboardActions = [];
	public Dictionary<int, int> MouseActions = [];
}

public class InputDataStore
{
	public List<KeyBinding> KeyboardActions = [];
	public List<MouseBinding> MouseActions = [];
}

[Nucleus.MarkForStaticConstruction]
public static class InputSettings
{
	const string DATASTORE_KEY = "CloneDash.InputSettings";
	const string MIGRATED_KEY = "CloneDash.InputSettings.MigratedToBinds";

	static readonly List<(string Set, InputAction Air, InputAction Ground, InputAction? Middle)> legacyShares = [];

	public static ConVar offset_visual = new(nameof(offset_visual), 0, FCvar.Saved, -500, 500);
	public static ConVar offset_judgement = new(nameof(offset_judgement), 0, FCvar.Saved, -500, 500);

	public static double VisualOffset => offset_visual.GetDouble() / 1000d;
	public static double JudgementOffset => offset_judgement.GetDouble() / 1000d;

	public static void Initialize() {
		if (Host.GetDataStore<bool>(MIGRATED_KEY))
			return;

		InputDataStore? data = null;
		try {
			var oldData = Host.GetDataStore<InputDataStore_SerializedBeforeFeb9th2026>(DATASTORE_KEY);
			if (oldData != null) {
				// Convert to the new form.
				data = new();
				foreach (var key in oldData.KeyboardActions)
					data.KeyboardActions.Add(new((ButtonCode)key.Key, key.Value));
				foreach (var btn in oldData.MouseActions)
					data.MouseActions.Add(new((ButtonCode)btn.Key, btn.Value));
			}
		}
		catch { }

		data ??= Host.GetDataStore<InputDataStore>(DATASTORE_KEY);
		if (data == null)
			return;

		var bindings = data.KeyboardActions.Select(binding => (binding.Key, binding.Action)).Concat(data.MouseActions.Select(binding => (binding.Button, binding.Action))).ToList();
		if (bindings.Count > 0)
			MigrateToBinds(bindings);
		Host.SetDataStore(MIGRATED_KEY, true);
	}

	public static void ShareLegacyBinds(string set, InputAction air, InputAction ground, InputAction? middle = null) => legacyShares.Add((set, air, ground, middle));

	static void MigrateToBinds(List<(ButtonCode Button, int Action)> bindings) {
		Key.UnbindAll(Key.COMMON);
		Key.UnbindAll(MuseDashActions.SET);
		foreach (var share in legacyShares)
			Key.UnbindAll(share.Set);

		foreach (var (button, action) in bindings) {
			switch (action) {
				case 0:
					Key.Bind(MuseDashActions.SET, button, MuseDashActions.Air.Command);
					foreach (var share in legacyShares)
						Key.Bind(share.Set, button, share.Air.Command);
					break;
				case 1:
					Key.Bind(MuseDashActions.SET, button, MuseDashActions.Ground.Command);
					foreach (var share in legacyShares)
						Key.Bind(share.Set, button, share.Ground.Command);
					break;
				case 2:
					Key.Bind(MuseDashActions.SET, button, MuseDashActions.Fever.Command);
					break;
				case 3:
					Key.Bind(Key.COMMON, button, CommonActions.Pause.Command);
					break;
				case 4:
					foreach (var share in legacyShares)
						if (share.Middle is InputAction middle)
							Key.Bind(share.Set, button, middle.Command);
					break;
			}
		}
	}
}
