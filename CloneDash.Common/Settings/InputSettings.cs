using Nucleus.Commands;
using Nucleus.Common.Input;
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
	public ButtonCode Key;
	public InputAction Action;

	public KeyBinding() { }
	public KeyBinding(ButtonCode key, InputAction action) {
		Key = key;
		Action = action;
	}
}
public record class MouseBinding
{
	public ButtonCode Button;
	public InputAction Action;

	public MouseBinding(ButtonCode btn, InputAction action) {
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
		store.KeyboardActions.Add(new(ButtonCode.KeyS, InputAction.AirAttack));
		store.KeyboardActions.Add(new(ButtonCode.KeyD, InputAction.AirAttack));
		store.KeyboardActions.Add(new(ButtonCode.KeyF, InputAction.AirAttack));
		store.KeyboardActions.Add(new(ButtonCode.KeyG, InputAction.AirAttack));

		store.KeyboardActions.Add(new(ButtonCode.KeyH, InputAction.GroundAttack));
		store.KeyboardActions.Add(new(ButtonCode.KeyJ, InputAction.GroundAttack));
		store.KeyboardActions.Add(new(ButtonCode.KeyK, InputAction.GroundAttack));
		store.KeyboardActions.Add(new(ButtonCode.KeyL, InputAction.GroundAttack));

		store.KeyboardActions.Add(new(ButtonCode.KeySpace, InputAction.FeverStart));
		store.KeyboardActions.Add(new(ButtonCode.KeyEscape, InputAction.PauseGame));

		store.MouseActions.Add(new(ButtonCode.MouseRight, InputAction.AirAttack));
		store.MouseActions.Add(new(ButtonCode.MouseLeft, InputAction.GroundAttack));
		store.ManualFever = false;
		return store;
	}

	public List<KeyBinding> KeyboardActions = [];
	public List<MouseBinding> MouseActions = [];
	public bool ManualFever = false;

	public bool IsKeyBound(ButtonCode key, out InputAction action) {
		foreach (var v in KeyboardActions)
			if (v.Key == key) {
				action = v.Action;
				return true;
			}
		action = default;
		return false;
	}
	public bool IsButtonCodeBound(ButtonCode button, out InputAction action) {
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
					data.KeyboardActions.Add(new((ButtonCode)key.Key, key.Value));
				foreach (var btn in oldData.MouseActions)
					data.MouseActions.Add(new((ButtonCode)btn.Key, btn.Value));

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

	public static bool IsKeyBound(ButtonCode key, out InputAction action) {
		if (data.IsKeyBound(key, out action))
			return true;
		return false;
	}

	public static bool IsButtonCodeBound(ButtonCode btn, out InputAction action) {
		if (data.IsButtonCodeBound(btn, out action))
			return true;
		return false;
	}

	public static IEnumerable<ButtonCode> GetKeysOfAction(InputAction action) {
		foreach (var key in data.KeyboardActions)
			if (key.Action == action)
				yield return key.Key;
	}

	public static IEnumerable<ButtonCode> GetButtonCodesOfAction(InputAction action) {
		foreach (var btn in data.MouseActions)
			if (btn.Action == action)
				yield return btn.Button;
	}

	public static void BindKey(ButtonCode key, InputAction action) {
		if (data.IsKeyBound(key, out _))
			UnbindKey(key);

		data.KeyboardActions.Add(new(key, action));
		Store();
	}

	public static bool RebindKey(ButtonCode keyReplace, ButtonCode keyWith, InputAction action) {
		UnbindKey(keyWith);

		for (int i = 0; i < data.KeyboardActions.Count; i++) {
			if (data.KeyboardActions[i].Key == keyReplace) {
				data.KeyboardActions[i].Key = keyWith;
				data.KeyboardActions[i].Action = action;
				Store();
				return true;
			}
		}

		return false;
	}

	public static bool UnbindKey(ButtonCode key) {
		for (int i = 0; i < data.KeyboardActions.Count; i++) {
			if (data.KeyboardActions[i].Key == key) {
				data.KeyboardActions.RemoveAt(i);
				Store();
				return true;
			}
		}

		return false;
	}

	public static void BindMouseCode(ButtonCode btn, InputAction action) {
		if (data.IsButtonCodeBound(btn, out _))
			UnbindMouseCode(btn);

		data.MouseActions.Add(new(btn, action));
		Store();
	}

	public static bool RebindMouseCode(ButtonCode btnReplace, ButtonCode btnWith, InputAction action) {
		UnbindMouseCode(btnWith);

		for (int i = 0; i < data.MouseActions.Count; i++) {
			if (data.MouseActions[i].Button == btnReplace) {
				data.MouseActions[i].Button = btnWith;
				data.MouseActions[i].Action = action;
				Store();
				return true;
			}
		}

		return false;
	}

	public static bool UnbindMouseCode(ButtonCode btn) {
		for (int i = 0; i < data.MouseActions.Count; i++) {
			if (data.MouseActions[i].Button == btn) {
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