using CloneDash.Settings;
using Nucleus;
using Nucleus.Types;

namespace CloneDash.Game.Input
{
	/// <summary>
	/// Player input interface. Allows for easily defining different input types, all that needs to be done is implementing this interface and modifying the <see cref="InputState"/> from <see cref="Poll(ref InputState)"/>
	/// </summary>
	public interface ICloneDashInputSystem : IInputReceiver
	{
		public static ICloneDashInputSystem[] InstantiateAllInputSystems() {
			var inputInterface = typeof(ICloneDashInputSystem);
			var inputs = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
				.Select(x => Activator.CreateInstance(x)).Select(x => (ICloneDashInputSystem?)x).ToArray();
			return inputs;
		}
	}
}
