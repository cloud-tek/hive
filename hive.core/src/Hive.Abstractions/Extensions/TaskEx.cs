namespace Hive.Extensions;

public static class TaskEx
{
    /// <summary>
    /// Blocks while condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The condition that will perpetuate the block.</param>
    /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <exception cref="TimeoutException"></exception>
    /// <returns></returns>
    public static async Task<bool> TryWaitWhile(Func<bool> condition, TimeSpan frequency, TimeSpan timeout, Action onSuccess = null)
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
    /// <returns></returns>
    public static async Task<bool> TryWaitUntil(Func<bool> condition, TimeSpan frequency, TimeSpan timeout, Action onFailure = null)
    {
        var waitTask = Task.Run(async () =>
        {
            while (!condition())
            {
                onFailure?.Invoke();
                await Task.Delay(frequency);
            }
        });

        if (waitTask != await Task.WhenAny(waitTask,
                Task.Delay(timeout)).ConfigureAwait(false))
            return false;

        return true;
    }
}
