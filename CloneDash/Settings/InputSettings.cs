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
	PauseGame,
    GiveUp // new action: just stop
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

public class InputDataStore
{
	public static InputDataStore NewStockSettings() {
		InputDataStore store = new();
        // why do we fight? why do we struggle?
        // controls are an illusion of control.

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
        store.KeyboardActions.Add(new(ButtonCode.KeyDelete, InputAction.GiveUp)); // escape is not enough

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
		data = Host.GetDataStore<InputDataStore>("CloneDash.InputSettings") ?? InputDataStore.NewStockSettings();
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
}
