namespace Hive.Extensions;

/// <summary>
/// Extensions for <see cref="Task"/>
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public static class TaskEx
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
  /// <summary>
  /// Blocks while condition is true or timeout occurs.
  /// </summary>
  /// <param name="condition">The condition that will perpetuate the block.</param>
  /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
  /// <param name="timeout">Timeout in milliseconds.</param>
  /// <param name="onSuccess">Action to fire whenever the condition predicate is met</param>
  /// <exception cref="TimeoutException">When a timeout occurs</exception>
  /// <returns>boolean <see cref="Task"/></returns>
  public static async Task<bool> TryWaitWhile(Func<bool> condition, TimeSpan frequency, TimeSpan timeout, Action onSuccess = default!)
  {
    var waitTask = Task.Run(async () =>
    {
      while (condition())
      {
        onSuccess?.Invoke();
        await Task.Delay(frequency).ConfigureAwait(false);
      }
    });

    if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
      return false;

    return true;
  }

  /// <summary>
  /// Blocks until condition is true or timeout occurs. Does not Throw
  /// </summary>
  /// <param name="condition">The break condition.</param>
  /// <param name="frequency">The frequency at which the condition will be checked.</param>
  /// <param name="timeout">The timeout in milliseconds.</param>
  /// <param name="onFailure">Action to fire whenever the condition predicate is not met</param>
  /// <returns>boolean <see cref="Task"/></returns>
  public static async Task<bool> TryWaitUntil(Func<bool> condition, TimeSpan frequency, TimeSpan timeout, Action onFailure = default!)
  {
    var waitTask = Task.Run(async () =>
    {
      while (!condition())
      {
        onFailure?.Invoke();
        await Task.Delay(frequency);
      }
    });

    if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)).ConfigureAwait(false))
      return false;

    return true;
  }
}