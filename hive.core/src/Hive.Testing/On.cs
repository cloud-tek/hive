namespace Hive.Testing;

/// <summary>
/// Represents the operating system on which the test should be executed
/// </summary>
[Flags]
public enum On
{
  /// <summary>
  /// Execute the test on all operating systems
  /// </summary>
  All = 0,

  /// <summary>
  /// Execute the test only on Windows
  /// </summary>
  Windows = 1,

  /// <summary>
  /// Execute the test only on Linux
  /// </summary>
  Linux = 2,

  /// <summary>
  /// Execute the test only on macOS
  /// </summary>
  MacOS = 4
}