namespace Hive.Testing;

/// <summary>
/// Temporarily sets an environment variable to a value and resets it when disposed.
/// </summary>
public class EnvironmentVariableScope : IDisposable
{
  private readonly string _name;

  private EnvironmentVariableScope(string name, string value)
  {
    _name = name ?? throw new ArgumentNullException(nameof(name));

    Environment.SetEnvironmentVariable(_name, value, EnvironmentVariableTarget.Process);
  }

  /// <summary>
  /// Sets the environment variable to the specified value
  /// </summary>
  /// <param name="name"></param>
  /// <param name="value"></param>
  /// <returns><see cref="IDisposable"/>></returns>
  public static IDisposable Create(string name, string value)
  {
    return new EnvironmentVariableScope(name, value);
  }

  /// <summary>
  /// Resets the environment variable to its original value
  /// </summary>
  public void Dispose()
  {
    Environment.SetEnvironmentVariable(_name, null, EnvironmentVariableTarget.Process);
    GC.SuppressFinalize(this);
  }
}