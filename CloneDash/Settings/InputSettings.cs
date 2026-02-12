using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Input;

namespace CloneDash.Settings;

public enum InputAction
{
	AirAttack,
	GroundAttack,
	FeverStart,
	PauseGame
}

public record class KeyBinding
{
	public int Key;
	public InputAction Action;

	public KeyBinding() { }
	public KeyBinding(int key, InputAction action) {
		Key = key;
		Action = action;
	}
}
public record class MouseBinding
{
	public int Button;
	public InputAction Action;

	public MouseBinding(int btn, InputAction action) {
		Button = btn;
		Action = action;
	}
}

// Sucks, but we made mistakes in the serialized data, and this is the cleanest way to handle those mistakes.
public class InputDataStore_SerializedBeforeFeb9th2026
{
	public Dictionary<int, InputAction> KeyboardActions;
	public Dictionary<int, InputAction> MouseActions;
	public bool ManualFever;
}

public class InputDataStore
{
	public static InputDataStore NewStockSettings() {
		InputDataStore store = new();
		store.KeyboardActions.Add(new(KeyboardLayout.USA.S.Key, InputAction.AirAttack));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.D.Key, InputAction.AirAttack));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.F.Key, InputAction.AirAttack));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.G.Key, InputAction.AirAttack));

		store.KeyboardActions.Add(new(KeyboardLayout.USA.H.Key, InputAction.GroundAttack));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.J.Key, InputAction.GroundAttack));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.K.Key, InputAction.GroundAttack));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.L.Key, InputAction.GroundAttack));

		store.KeyboardActions.Add(new(KeyboardLayout.USA.Space.Key, InputAction.FeverStart));
		store.KeyboardActions.Add(new(KeyboardLayout.USA.Escape.Key, InputAction.PauseGame));

		store.MouseActions.Add(new(MouseButton.MouseRight.Button, InputAction.AirAttack));
		store.MouseActions.Add(new(MouseButton.MouseLeft.Button, InputAction.GroundAttack));
		store.ManualFever = false;
		return store;
	}

	public List<KeyBinding> KeyboardActions = [];
	public List<MouseBinding> MouseActions = [];
	public bool ManualFever = false;

	public bool IsKeyBound(int key, out InputAction action) {
		foreach (var v in KeyboardActions)
			if (v.Key == key) {
				action = v.Action;
				return true;
			}
		action = default;
		return false;
	}
	public bool IsMouseButtonBound(int button, out InputAction action) {
		foreach (var v in MouseActions)
			if (v.Button == button) {
				action = v.Action;
				return true;
			}
		action = default;
		return false;
	}
}

[Nucleus.MarkForStaticConstruction]
public static class InputSettings
{
	private static InputDataStore data;

	public static ConVar offset_visual = new(nameof(offset_visual), 0, FCvar.Saved, -500, 500);
	public static ConVar offset_judgement = new(nameof(offset_judgement), 0, FCvar.Saved, -500, 500);


	static InputSettings() {
		try {
			var oldData = Host.GetDataStore<InputDataStore_SerializedBeforeFeb9th2026>("CloneDash.InputSettings");
			if (oldData != null) {
				// Convert to the new form.
				data = new();

				data.KeyboardActions.Clear();
				data.MouseActions.Clear();
				foreach (var key in oldData.KeyboardActions)
					data.KeyboardActions.Add(new(key.Key, key.Value));
				foreach (var btn in oldData.MouseActions)
					data.MouseActions.Add(new(btn.Key, btn.Value));

				data.ManualFever = oldData.ManualFever;
			}
		}
		catch { }

		data ??= Host.GetDataStore<InputDataStore>("CloneDash.InputSettings") ?? InputDataStore.NewStockSettings();
		Store();
	}
	public static void Store() {
		Host.SetDataStore("CloneDash.InputSettings", data);
		OnSettingsChanged?.Invoke();
	}

	public delegate void SettingsChanged();
	public static event SettingsChanged? OnSettingsChanged;

	public static bool IsKeyBound(KeyboardKey key, out InputAction action) {
		if (data.IsKeyBound(key.Key, out action))
			return true;
		return false;
	}

	public static bool IsMouseButtonBound(MouseButton btn, out InputAction action) {
		if (data.IsMouseButtonBound(btn.Button, out action))
			return true;
		return false;
	}

	public static IEnumerable<KeyboardKey> GetKeysOfAction(InputAction action) {
		foreach (var key in data.KeyboardActions)
			if (key.Action == action)
				yield return KeyboardLayout.USA.FromInt(key.Key);
	}

	public static IEnumerable<MouseButton> GetMouseButtonsOfAction(InputAction action) {
		foreach (var btn in data.MouseActions)
			if (btn.Action == action)
				yield return new(btn.Button);
	}

	public static void BindKey(KeyboardKey key, InputAction action) {
		if (data.IsKeyBound(key.Key, out _))
			UnbindKey(key);

		data.KeyboardActions.Add(new(key.Key, action));
		Store();
	}

	public static bool RebindKey(KeyboardKey keyReplace, KeyboardKey keyWith, InputAction action) {
		UnbindKey(keyWith);

		for (int i = 0; i < data.KeyboardActions.Count; i++) {
			if (data.KeyboardActions[i].Key == keyReplace.Key) {
				data.KeyboardActions[i].Key = keyWith.Key;
				data.KeyboardActions[i].Action = action;
				Store();
				return true;
			}
		}

		return false;
	}

	public static bool UnbindKey(KeyboardKey key) {
		for (int i = 0; i < data.KeyboardActions.Count; i++) {
			if (data.KeyboardActions[i].Key == key.Key) {
				data.KeyboardActions.RemoveAt(i);
				Store();
				return true;
			}
		}

		return false;
	}

	public static void BindMouseButton(MouseButton btn, InputAction action) {
		if (data.IsMouseButtonBound(btn.Button, out _))
			UnbindMouseButton(btn);

		data.MouseActions.Add(new(btn.Button, action));
		Store();
	}

	public static bool RebindMouseButton(MouseButton btnReplace, MouseButton btnWith, InputAction action) {
		UnbindMouseButton(btnWith);

		for (int i = 0; i < data.MouseActions.Count; i++) {
			if (data.MouseActions[i].Button == btnReplace.Button) {
				data.MouseActions[i].Button = btnWith.Button;
				data.MouseActions[i].Action = action;
				Store();
				return true;
			}
		}

		return false;
	}

	public static bool UnbindMouseButton(MouseButton btn) {
		for (int i = 0; i < data.MouseActions.Count; i++) {
			if (data.MouseActions[i].Button == btn.Button) {
				data.MouseActions.RemoveAt(i);
				Store();
				return true;
			}
		}

		return false;
	}
	public static bool ManualFever {
		get => data.ManualFever;
		set {
			data.ManualFever = value;
			Store();
		}
	}

	public static double VisualOffset => offset_visual.GetDouble() / 1000d;
	public static double JudgementOffset => offset_judgement.GetDouble() / 1000d;
}