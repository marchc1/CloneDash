using CloneDash.Common.Game;
using Nucleus.Common.Input;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Input;

public class BindInput(InputComponent input) : IInputReceiver
{
	readonly KeyBindSet common = Key.GetSet(Key.COMMON);

	public void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null) {
		Poll(common, frameState, ref inputState, actionFilter);
		if (input.Binds != null)
			Poll(input.Binds, frameState, ref inputState, actionFilter);
	}

	static void Poll(KeyBindSet binds, FrameState frameState, ref InputState inputState, InputAction? actionFilter) {
		foreach (var binding in binds.Actions) {
			if (binding.Action >= InputState.ACTION_SLOTS || (actionFilter is InputAction filter && filter.Slot != binding.Action))
				continue;

			var action = new InputAction(binding.Action);
			if (binding.Button.IsMouseCode()) {
				inputState.Click(action, frameState.Mouse.Clicked(binding.Button) ? 1 : 0);
				inputState.Hold(action, frameState.Mouse.Held(binding.Button) ? 1 : 0);
			}
			else {
				inputState.Click(action, frameState.Keyboard.WasKeyPressed(binding.Button) ? 1 : 0);
				inputState.Hold(action, frameState.Keyboard.IsKeyDown(binding.Button) ? 1 : 0);
			}
		}
	}
}
