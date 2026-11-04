using CloneDash.Settings;
using Nucleus.Types;

namespace CloneDash.Game.Input
{
	public interface IInputReceiver
	{
		bool ClaimsGameplayInput => false;

		/// <summary>
		/// Will be called every tick.<br></br>
		/// You should avoid raw-setting data to these fields (ie. avoid <c><see cref="InputState.Clicked"/>[action] = 2</c>), because this will interfere with other input interfaces. <br></br>
		/// Use += for integer-fields and |= for boolean fields, so integer-based inputs (like how many clicks happened this frame) are additive, and boolean-based inputs (like if the pause button was pressed) are true if any input interface said it was true.<br></br>
		/// So instead, you would want to do <c><see cref="InputState.Click"/>(action, 2)</c>, <c><see cref="InputState.PauseButton"/> |= false</c>, etc...
		/// </summary>
		void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null);
	}
}
