using Nucleus.Core;
using Nucleus.Input;
using static CloneDash.Game.Input.CD_InputSettings;

namespace CloneDash.Game.Input;

public enum CD_InputAction {
	AirAttack,
	GroundAttack,
	FeverStart,
	PauseGame
}
public class CD_InputDataStore {
	public Dictionary<int, CD_InputAction> KeyboardActions = new() {
		{ KeyboardLayout.USA.S.Key, CD_InputAction.AirAttack },
		{ KeyboardLayout.USA.D.Key, CD_InputAction.AirAttack },
		{ KeyboardLayout.USA.F.Key, CD_InputAction.AirAttack },
		{ KeyboardLayout.USA.G.Key, CD_InputAction.AirAttack },

		{ KeyboardLayout.USA.H.Key, CD_InputAction.GroundAttack },
		{ KeyboardLayout.USA.J.Key, CD_InputAction.GroundAttack },
		{ KeyboardLayout.USA.K.Key, CD_InputAction.GroundAttack },
		{ KeyboardLayout.USA.L.Key, CD_InputAction.GroundAttack },

		{ KeyboardLayout.USA.Space.Key, CD_InputAction.FeverStart },
		{ KeyboardLayout.USA.Escape.Key, CD_InputAction.PauseGame }
	};
	public Dictionary<int, CD_InputAction> MouseActions = new() {
		{ MouseButton.MouseRight.Button, CD_InputAction.AirAttack },
		{ MouseButton.MouseLeft.Button, CD_InputAction.GroundAttack }
	};
	public bool ManualFever = false;
}

[Nucleus.MarkForStaticConstruction]
public static class CD_InputSettings
{
	private static CD_InputDataStore data;

	static CD_InputSettings() { 
		data = Host.GetDataStore<CD_InputDataStore>("CloneDash.InputSettings") ?? new();
		Store();
	}
	public static void Store() {
		Host.SetDataStore("CloneDash.InputSettings", data);
		OnSettingsChanged?.Invoke();
		Host.WriteConfig();
	}

	public delegate void SettingsChanged();
	public static event SettingsChanged? OnSettingsChanged;

	public static bool IsKeyBound(KeyboardKey key, out CD_InputAction action) {
		if (data.KeyboardActions.TryGetValue(key.Key, out action))
			return true;
		return false;
	}

	public static bool IsMouseButtonBound(MouseButton btn, out CD_InputAction action) {
		if (data.MouseActions.TryGetValue(btn.Button, out action))
			return true;
		return false;
	}

	public static IEnumerable<KeyboardKey> GetKeysOfAction(CD_InputAction action) {
		foreach (var key in data.KeyboardActions)
			if (key.Value == action)
				yield return KeyboardLayout.USA.FromInt(key.Key);
	}
	public static IEnumerable<MouseButton> GetMouseButtonsOfAction(CD_InputAction action) {
		foreach (var btn in data.MouseActions)
			if (btn.Value == action)
				yield return new(btn.Key);
	}

	public static void BindKey(KeyboardKey key, CD_InputAction action) {
		data.KeyboardActions[key.Key] = action;
		Store();
	}
	public static void UnbindKey(KeyboardKey key) {
		data.KeyboardActions.Remove(key.Key);
		Store();
	}
	public static void BindMouseButton(MouseButton btn, CD_InputAction action) {
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
