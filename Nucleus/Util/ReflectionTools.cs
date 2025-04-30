using System.Reflection;

namespace Nucleus.Util;

public static class ReflectionTools
{
	public static Type[] GetInheritorsOfAbstractType(this Type type)
						=> Assembly.GetAssembly(type).GetTypes()
						.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(type))
						.ToArray();
	public static T[] InstantiateAllInheritorsOfAbstractType<T>() {
		var inheritors = typeof(T).GetInheritorsOfAbstractType();
		T[] ret = new T[inheritors.Length];
		for (int i = 0; i < inheritors.Length; i++) {
			ret[i] = (T)Activator.CreateInstance(inheritors[i]);
		}
		return ret;
	}
}
