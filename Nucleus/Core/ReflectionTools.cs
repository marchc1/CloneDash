using System.Reflection;

namespace Nucleus;

public static class ReflectionTools
{
	public static Type[] GetInheritorsOfAbstractType(this Type type)
						=> Assembly.GetAssembly(type).GetTypes()
						.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(type))
						.ToArray();
}
