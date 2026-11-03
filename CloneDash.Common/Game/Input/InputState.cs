using CloneDash.Settings;
using System.Runtime.CompilerServices;

namespace CloneDash.Game.Input
{
	[InlineArray(InputState.ACTION_SLOTS)]
	public struct InputActionCounts
	{
		int element;
	}

	public struct InputState
	{
		public const int ACTION_SLOTS = 32;

		/// <summary>
		/// How many times each action's keys were clicked this frame, indexed by <see cref="InputAction"/>
		/// </summary>
		public InputActionCounts Clicked;

		/// <summary>
		/// How many of each action's keys are held, indexed by <see cref="InputAction"/>
		/// </summary>
		public InputActionCounts HeldCount;

		/// <summary>
		/// Was the fever key pressed?
		/// </summary>
		public bool TryFever;

		/// <summary>
		/// Was the pause button pressed?
		/// </summary>
		public bool PauseButton;

		public InputState() {
			Reset();
		}

		public readonly int ClickCount(InputAction action) => Clicked[(int)action];
		public readonly int HeldKeys(InputAction action) => HeldCount[(int)action];
		public readonly bool IsHeld(InputAction action) => HeldCount[(int)action] > 0;

		public void Click(InputAction action, int count = 1) => Clicked[(int)action] += count;
		public void Hold(InputAction action, int count = 1) => HeldCount[(int)action] += count;

		public void Reset() {
			Clicked = default;
			HeldCount = default;
			TryFever = false;
			PauseButton = false;
		}
	}
}
