using CloneDash.Settings;
using Nucleus;
using Nucleus.Types;

namespace CloneDash.Game.Input
{
	/// <summary>
	/// Player input interface. Allows for easily defining different input types, all that needs to be done is implementing this interface and modifying the <see cref="InputState"/> from <see cref="Poll(ref InputState)"/>
	/// </summary>
	public interface ICloneDashInputSystem
	{
		public static ICloneDashInputSystem[] InstantiateAllInputSystems() {
			var inputInterface = typeof(ICloneDashInputSystem);
			var inputs = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
				.Select(x => Activator.CreateInstance(x)).Select(x => (ICloneDashInputSystem?)x).ToArray();
			return inputs;
		}

		/// <summary>
		/// Will be called every tick.<br></br>
		/// You should avoid raw-setting data to these fields (ie. avoid <c><see cref="InputState.TopClicked"/> = 2</c>), because this will interfere with other input interfaces. <br></br>
		/// Use += for integer-fields and |= for boolean fields, so integer-based inputs (like how many clicks happened this frame) are additive, and boolean-based inputs (like if the pause button was pressed) are true if any input interface said it was true.<br></br>
		/// So instead, you would want to do <c><see cref="InputState.TopClicked"/> += 2</c>, <c><see cref="InputState.PauseButton"/> |= false</c>, etc...
		/// </summary>
		void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null);
	}
}
