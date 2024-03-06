namespace Hive.Extensions;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>
/// </summary>
public static class EnumerableExtensions
{
  /// <summary>
  /// Performs the specified action on each element of the <see cref="IEnumerable{T}"/>
  /// </summary>
  /// <typeparam name="T">Type of the enumerable</typeparam>
  /// <param name="enumerable"></param>
  /// <param name="action"></param>
  /// <exception cref="ArgumentNullException">When any of the provided arguments is null</exception>
  public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
  {
    _ = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
    _ = action ?? throw new ArgumentNullException(nameof(action));

    foreach (var item in enumerable)
    {
      action(item);
    }
  }

  /// <summary>
  /// Returns a random element from the <see cref="IEnumerable{T}"/>
  /// </summary>
  /// <typeparam name="T">Type of the enumerable</typeparam>
  /// <param name="enumerable"></param>
  /// <param name="rand"></param>
  /// <returns>Random element from the IEnumerable</returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments is null</exception>
  /// <exception cref="InvalidOperationException">When the enumerable contains no elements</exception>
  public static T? Random<T>(this IEnumerable<T> enumerable, Random rand)
  {
    _ = enumerable ?? throw new ArgumentNullException(nameof(enumerable));

    var current = default(T);
    var count = 0;

    foreach (var element in @enumerable)
    {
      count++;
      if (rand.Next(count) == 0)
      {
        current = element;
      }
    }

    if (count == 0)
    {
      throw new InvalidOperationException("Sequence contains no elements");
    }

    return current;
  }

  /// <summary>
  /// Checks if the <see cref="IEnumerable{T}"/> is null or empty
  /// </summary>
  /// <typeparam name="T">Type of the enumerable</typeparam>
  /// <param name="enumerable"></param>
  /// <returns>boolean</returns>
  public static bool IsNullOrEmpty<T>(this IEnumerable<T> @enumerable)
  {
    return (enumerable == null) || !enumerable.Any();
  }
}