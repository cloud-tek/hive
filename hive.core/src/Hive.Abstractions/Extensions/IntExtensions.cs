namespace Hive.Extensions;

/// <summary>
/// Extensions for <see cref="int"/>
/// </summary>
public static class IntExtensions
{
  /// <summary>
  /// Converts the int to a <see cref="TimeSpan"/> representing the number of seconds
  /// </summary>
  /// <param name="value"></param>
  /// <returns><see cref="TimeSpan"/></returns>
  public static TimeSpan Seconds(this int value)
  {
    return TimeSpan.FromSeconds(value);
  }

  /// <summary>
  /// Converts the int to a <see cref="TimeSpan"/> representing the number of milliseconds
  /// </summary>
  /// <param name="value"></param>
  /// <returns><see cref="TimeSpan"/></returns>
  public static TimeSpan Milliseconds(this int value)
  {
    return TimeSpan.FromMilliseconds(value);
  }
}