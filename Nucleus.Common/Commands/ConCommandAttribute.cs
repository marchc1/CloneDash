using Nucleus.Common.Commands;
using System.Reflection;

namespace Nucleus.Commands
{
	// Registers a method as a ConCommand via attribute
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class ConCommandAttribute : Attribute
	{
		public readonly string? NameOverride;
		public readonly string Description;
		public readonly string? AutoComplete;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Name">Name override. By default, the name is pulled from the method name</param>
		/// <param name="Help">The help text for the user</param>
		/// <param name="autoComplete">Optional name of an autocomplete delegate, contained in the same class. Must be static! (use nameof(autocompleteFunc))</param>
		public ConCommandAttribute(string? Name = null, string Help = "", string? autoComplete = null) {
			NameOverride = Name;
			Description = Help;
			AutoComplete = autoComplete;
		}
		public static IEnumerable<(MethodInfo baseMethod, ConCommandAttribute attr)> GetAttributes(Type t) {
			foreach (var method in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
				ConCommandAttribute? attr = method.GetCustomAttribute<ConCommandAttribute>();
				if (attr == null) continue;

				yield return new(method, attr);
			}
		}
		public static void RegisterAttribute(Type baseType, MethodInfo baseMethod, ConCommandAttribute attr) {
			CommandExecutedDelegate executedDelegate;
			var parameters = baseMethod.GetParameters();

			if (parameters.Length == 1 && parameters[0].ParameterType == typeof(TokenizedCommand).MakeByRefType())
				executedDelegate = (_, in args) => baseMethod.Invoke(null, [args]);
			else if (parameters.Length == 0)
				executedDelegate = (_, in _) => baseMethod.Invoke(null, null);
			else
				executedDelegate = baseMethod.CreateDelegate<CommandExecutedDelegate>();

			ConCommand store;
			if (attr.AutoComplete == null)
				store = new ConCommand(attr.NameOverride ?? baseMethod.Name, executedDelegate, null, attr.Description);
			else
				store = new ConCommand(attr.NameOverride ?? baseMethod.Name, executedDelegate, baseType.GetMethod(attr.AutoComplete)!.CreateDelegate<AutocompleteDelegate>(), attr.Description);
			// The concommand created will not be GC'd after this; it's stored in the internal ConCommandBase linked list.
		}
	}
}
