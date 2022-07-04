namespace Hive.Extensions;

public static class IntExtensions
{
    public static TimeSpan Seconds(this int @int)
    {
        return TimeSpan.FromSeconds(@int);
    }

    public static TimeSpan Milliseconds(this int @int)
    {
        return TimeSpan.FromMilliseconds(@int);
    }
}
