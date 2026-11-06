using Nucleus;

namespace CloneDash.Common.Game;

public abstract class ContentAdapter<TFrom, TTo>(TFrom source) where TFrom : class where TTo : class
{
	public TFrom Source { get; } = source;
}

public static class ContentAdapters
{
	static List<(Type From, Type To, Type Adapter)>? adapters;

	public static IReadOnlyList<(Type From, Type To, Type Adapter)> All => adapters ??= Discover();

	public static Type? Find(Type runtimeType, Type contract) {
		foreach (var (from, to, adapter) in All)
			if (from.IsAssignableFrom(runtimeType) && contract.IsAssignableFrom(to))
				return adapter;
		return null;
	}

	static List<(Type From, Type To, Type Adapter)> Discover() {
		List<(Type From, Type To, Type Adapter)> found = [];
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
			Type[] types;
			try {
				types = assembly.GetTypes();
			}
			catch {
				continue;
			}

			foreach (var type in types) {
				if (type.IsAbstract || type.IsInterface || FindBase(type) is not Type baseType)
					continue;

				var args = baseType.GetGenericArguments();
				if (!args[1].IsAssignableFrom(type)) {
					Logs.Warn($"Content adapter {type.Name} doesn't implement {args[1].Name}, so it can't be used.");
					continue;
				}
				found.Add((args[0], args[1], type));
			}
		}
		return found;
	}

	static Type? FindBase(Type type) {
		for (var current = type.BaseType; current != null; current = current.BaseType)
			if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ContentAdapter<,>))
				return current;
		return null;
	}
}

public class ContentSlotException(string message) : Exception(message);
