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
public class InputDataStore
{
	public Dictionary<int, InputAction> KeyboardActions = new() {
		{ KeyboardLayout.USA.S.Key, InputAction.AirAttack },
		{ KeyboardLayout.USA.D.Key, InputAction.AirAttack },
		{ KeyboardLayout.USA.F.Key, InputAction.AirAttack },
		{ KeyboardLayout.USA.G.Key, InputAction.AirAttack },

		{ KeyboardLayout.USA.H.Key, InputAction.GroundAttack },
		{ KeyboardLayout.USA.J.Key, InputAction.GroundAttack },
		{ KeyboardLayout.USA.K.Key, InputAction.GroundAttack },
		{ KeyboardLayout.USA.L.Key, InputAction.GroundAttack },

		{ KeyboardLayout.USA.Space.Key, InputAction.FeverStart },
		{ KeyboardLayout.USA.Escape.Key, InputAction.PauseGame }
	};
	public Dictionary<int, InputAction> MouseActions = new() {
		{ MouseButton.MouseRight.Button, InputAction.AirAttack },
		{ MouseButton.MouseLeft.Button, InputAction.GroundAttack }
	};
	public bool ManualFever = false;
}

[Nucleus.MarkForStaticConstruction]
public static class InputSettings
{
	private static InputDataStore data;

	static InputSettings() {
		data = Host.GetDataStore<InputDataStore>("CloneDash.InputSettings") ?? new();
		Store();
	}
	public static void Store() {
		Host.SetDataStore("CloneDash.InputSettings", data);
		OnSettingsChanged?.Invoke();
		Host.WriteConfig();
	}

	public delegate void SettingsChanged();
	public static event SettingsChanged? OnSettingsChanged;

	public static bool IsKeyBound(KeyboardKey key, out InputAction action) {
		if (data.KeyboardActions.TryGetValue(key.Key, out action))
			return true;
		return false;
	}

	public static bool IsMouseButtonBound(MouseButton btn, out InputAction action) {
		if (data.MouseActions.TryGetValue(btn.Button, out action))
			return true;
		return false;
	}

	public static IEnumerable<KeyboardKey> GetKeysOfAction(InputAction action) {
		foreach (var key in data.KeyboardActions)
			if (key.Value == action)
				yield return KeyboardLayout.USA.FromInt(key.Key);
	}

	public static IEnumerable<MouseButton> GetMouseButtonsOfAction(InputAction action) {
		foreach (var btn in data.MouseActions)
			if (btn.Value == action)
				yield return new(btn.Key);
	}

	public static void BindKey(KeyboardKey key, InputAction action) {
		data.KeyboardActions[key.Key] = action;
		Store();
	}
	public static void UnbindKey(KeyboardKey key) {
		data.KeyboardActions.Remove(key.Key);
		Store();
	}
	public static void BindMouseButton(MouseButton btn, InputAction action) {
		data.MouseActions[btn.Button] = action;
		Store();
	}
	public static void UnbindMouseButton(MouseButton btn) {
		data.MouseActions.Remove(btn.Button);
		Store();
	}
	public static bool ManualFever {
		get => data.ManualFever;
		set {
			data.ManualFever = value;
			Store();
		}
	}
}
