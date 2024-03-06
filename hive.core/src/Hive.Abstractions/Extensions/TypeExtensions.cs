using System.Reflection;

namespace Hive.Extensions;

/// <summary>
/// Extensions for <see cref="Type"/>
/// </summary>
public static class TypeExtensions
{
  /// <summary>
  /// Determines if the type has the specified attribute
  /// </summary>
  /// <typeparam name="TAttribute">Type of the attribute</typeparam>
  /// <param name="type"></param>
  /// <returns>boolean</returns>
  public static bool HasAttribute<TAttribute>(this Type type)
      where TAttribute : Attribute
  {
    return type.GetCustomAttribute<TAttribute>() != null;
  }
}