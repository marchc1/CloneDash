using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nucleus.Common;

public static class ReflectionUtils
{
	public static bool TryToDelegate<T>(this MethodInfo m, object? instance, [NotNullWhen(true)] out T? asDelegate) where T : Delegate {
		return (asDelegate =
			(T?)(instance == null
				? Delegate.CreateDelegate(typeof(T), m, false)
				: Delegate.CreateDelegate(typeof(T), instance, m, false))
			) != null;
	}

	public static bool TryExtractMethodDelegate<T>(this Type type, object? instance, Func<MethodInfo, bool> preFilter, [NotNullWhen(true)] out T? asDelegate) where T : Delegate {
		if (TryFindMatchingMethod(type, typeof(T), preFilter, out MethodInfo? methodInfo) && TryToDelegate(methodInfo, instance, out asDelegate))
			return true;

		asDelegate = null;
		return false;
	}

	public static bool DoesMethodMatch(this MethodInfo m, Type[] delegateParams, Type delegateReturn, Func<MethodInfo, bool>? preFilter = null) {
		if (preFilter != null)
			return preFilter(m);

		if (m.ReturnType != delegateReturn)
			return false;

		var methodParams = m.GetParameters().Select(p => p.ParameterType).ToArray();
		if (methodParams.Length != delegateParams.Length)
			return false;

		for (int i = 0; i < methodParams.Length; i++) {
			if (methodParams[i] != delegateParams[i])
				return false;
		}

		return true;
	}
	public static MethodInfo? FindMatchingMethod(this Type targetType, Type[] delegateParams, Type delegateReturn, Func<MethodInfo, bool>? preFilter = null)
		=> targetType
			.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
			.FirstOrDefault(m => DoesMethodMatch(m, delegateParams, delegateReturn, preFilter));
	public static MethodInfo? FindMatchingMethod(this Type targetType, Type delegateType, Func<MethodInfo, bool>? preFilter = null) {
		if (!typeof(Delegate).IsAssignableFrom(delegateType))
			throw new ArgumentException("delegateType must be a delegate", nameof(delegateType));

		var invoke = delegateType.GetMethod("Invoke")!;
		var delegateParams = invoke.GetParameters().Select(p => p.ParameterType).ToArray();
		var delegateReturn = invoke.ReturnType;
		return FindMatchingMethod(targetType, delegateParams, delegateReturn, preFilter);
	}
	public static MethodInfo? FindMatchingMethod<T>(this Type targetType, Func<MethodInfo, bool>? preFilter = null) where T : Delegate => FindMatchingMethod(targetType, typeof(T), preFilter);


	public static bool TryFindMatchingMethod(this Type targetType, Type[] delegateParams, Type delegateReturn, Func<MethodInfo, bool>? preFilter, [NotNullWhen(true)] out MethodInfo? info) {
		info = FindMatchingMethod(targetType, delegateParams, delegateReturn, preFilter);
		return info != null;
	}

	public static bool TryFindMatchingMethod(this Type targetType, Type delegateType, Func<MethodInfo, bool>? preFilter, [NotNullWhen(true)] out MethodInfo? info) {
		info = FindMatchingMethod(targetType, delegateType, preFilter);
		return info != null;
	}

	public static bool TryFindMatchingMethod<T>(this Type targetType, Func<MethodInfo, bool>? preFilter, [NotNullWhen(true)] out MethodInfo? info) where T : Delegate {
		info = FindMatchingMethod<T>(targetType, preFilter);
		return info != null;
	}
	static IEnumerable<Type> safeTypeGet(Assembly assembly) {
		if (!IsOkAssembly(assembly))
			yield break;

		IEnumerable<Type?> types;
		try {
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e) {
			types = e.Types;
		}

		foreach (var t in types.Where(t => t != null))
			yield return t!;
	}

	public static bool IsOkAssembly(Assembly assembly) {
		return true;
	}

	public static IEnumerable<Assembly> GetAssemblies()
		=> AppDomain.CurrentDomain.GetAssemblies().Where(IsOkAssembly);
	public static IEnumerable<Type> GetLoadedTypes()
		=> AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(safeTypeGet);

	public static IEnumerable<KeyValuePair<Type, T>> GetLoadedTypesWithAttribute<T>() where T : Attribute {
		foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(safeTypeGet)) {
			T? attr = type.GetCustomAttribute<T>();
			if (attr != null)
				yield return new(type, attr);
		}
	}

	public static IEnumerable<KeyValuePair<Type, T>> GetTypesWithAttribute<T>(this Assembly assembly) where T : Attribute {
		foreach (var type in assembly.GetTypes()) {
			T? attr = type.GetCustomAttribute<T>();
			if (attr != null)
				yield return new(type, attr);
		}
	}

	public static IEnumerable<KeyValuePair<Type, T>> GetTypesWithAttributeMulti<T>(this Assembly assembly) where T : Attribute {
		foreach (var type in assembly.GetTypes()) {
			foreach (var attr in type.GetCustomAttributes<T>())
				yield return new(type, attr);
		}
	}

	public static IEnumerable<KeyValuePair<ConstructorInfo, T>> GetConstructorsWithAttribute<T>(this Type type) where T : Attribute {
		foreach (var constructor in type.GetConstructors()) {
			T? attr = type.GetCustomAttribute<T>();
			if (attr != null)
				yield return new(constructor, attr);
		}
	}
	public static IEnumerable<KeyValuePair<PropertyInfo, T>> GetPropertiesWithAttribute<T>(this Type type) where T : Attribute {
		foreach (var prop in type.GetProperties()) {
			T? attr = type.GetCustomAttribute<T>();
			if (attr != null)
				yield return new(prop, attr);
		}
	}
	public static IEnumerable<KeyValuePair<FieldInfo, T>> GetFieldsWithAttribute<T>(this Type type) where T : Attribute {
		foreach (var field in type.GetFields()) {
			T? attr = type.GetCustomAttribute<T>();
			if (attr != null)
				yield return new(field, attr);
		}
	}
	public static IEnumerable<KeyValuePair<MethodInfo, T>> GetMethodsWithAttribute<T>(this Type type) where T : Attribute {
		foreach (var method in type.GetMethods()) {
			T? attr = type.GetCustomAttribute<T>();
			if (attr != null)
				yield return new(method, attr);
		}
	}
}
