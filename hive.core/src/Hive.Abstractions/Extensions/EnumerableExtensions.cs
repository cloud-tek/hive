namespace Hive.Extensions;

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
        if (action == null) throw new ArgumentNullException(nameof(action));

        foreach (var item in enumerable)
        {
            action(item);
        }
    }

    public static T? Random<T>(this IEnumerable<T> @enumerable, Random rand)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

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

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> @enumerable)
    {
      return (enumerable == null) || !enumerable.Any();
    }
}
