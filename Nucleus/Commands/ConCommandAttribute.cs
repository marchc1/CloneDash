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
		internal static void RegisterAttribute(Type baseType, MethodInfo baseMethod, ConCommandAttribute attr) {
			ConCommand.ExecutedDelegate executedDelegate;
			var parameters = baseMethod.GetParameters();

			if (parameters.Length == 1 && parameters[0].ParameterType == typeof(ConCommandArguments))
				executedDelegate = (_, args) => baseMethod.Invoke(null, [args]);
			else if (parameters.Length == 0)
				executedDelegate = (_, _) => baseMethod.Invoke(null, null);
			else
				executedDelegate = baseMethod.CreateDelegate<ConCommand.ExecutedDelegate>();

			if (attr.AutoComplete == null)
				ConCommand.Register(attr.NameOverride ?? baseMethod.Name, executedDelegate, attr.Description);
			else
				ConCommand.Register(attr.NameOverride ?? baseMethod.Name, executedDelegate, baseType.GetMethod(attr.AutoComplete)!.CreateDelegate<ConCommandBase.AutocompleteDelegate>(), attr.Description);
		}
	}
}
