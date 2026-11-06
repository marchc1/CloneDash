using Nucleus.Common.Input;
using Nucleus.Engine;

namespace CloneDash.Game.Input;

public readonly record struct InputAction(int Slot)
{
	public string Command => $"+{Name}";
	public string Name => Key.GetActionName(Slot);

	public static InputAction Register(string name) {
		int slot = Key.RegisterAction(name);
		if (slot >= InputState.ACTION_SLOTS)
			throw new InvalidOperationException($"Input action '{name}' doesn't fit in {InputState.ACTION_SLOTS} action slots.");
		return new(slot);
	}

	public void BindDefault(string set, params ButtonCode[] buttons) {
		foreach (var button in buttons)
			Key.RegisterDefault(set, button, Command);
	}

	public override string ToString() => Name;
}
