using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Nucleus.Util;

public static class ReflectionTools
{
	public static Type[] GetInheritorsOfAbstractType(this Type type)
						=> Assembly.GetAssembly(type)!.GetTypes()
						.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(type))
						.ToArray();

	public static T[] InstantiateAllInheritorsOfInterface<T>() => AppDomain.CurrentDomain
			.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
			.Where(t => t.GetConstructor(Type.EmptyTypes) != null)
			.Select(t => (T)Activator.CreateInstance(t)!)
			.ToArray();

	public static T[] InstantiateAllInheritorsOfAbstractType<T>() {
		var inheritors = typeof(T).GetInheritorsOfAbstractType();
		T[] ret = new T[inheritors.Length];
		for (int i = 0; i < inheritors.Length; i++) {
			ret[i] = (T)Activator.CreateInstance(inheritors[i])!;
		}
		return ret;
	}

	public static bool IsAssemblyDebugBuild(this Assembly assembly) {
		return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
	}

	public static DateTime? GetLinkerTime(this Assembly assembly) {
		const string BuildVersionMetadataPrefix = "+build";

		var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
		if (attribute?.InformationalVersion != null) {
			var value = attribute.InformationalVersion;
			var index = value.IndexOf(BuildVersionMetadataPrefix);
			if (index > 0) {
				value = value[(index + BuildVersionMetadataPrefix.Length)..];
				return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
			}
		}

		return null;
	}

	public static bool TryGetLinkerTime(this Assembly assembly, [NotNullWhen(true)] out DateTime dateTime) {
		var dt = GetLinkerTime(assembly);
		dateTime = dt ?? default;
		return dt.HasValue;
	}
}
