using System.Reflection;

namespace Hive.Extensions;

public static class TypeExtensions
{
    public static bool HasAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        return type.GetCustomAttribute<TAttribute>() != null;
    }
}
