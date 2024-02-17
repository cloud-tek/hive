namespace Hive.Testing;

/// <summary>
/// Extends the XUnit TheoryAttribute to support conditional execution
/// </summary>
public sealed class SmartTheoryAttribute : Xunit.TheoryAttribute
{
  /// <summary>
  /// Creates a new instance of <see cref="SmartTheoryAttribute"/> and constrains execution to a specific platform
  /// </summary>
  /// <param name="on"></param>
  public SmartTheoryAttribute(On on)
  {
    Skip = TestExecutionResolver.Resolve(on);
  }

  /// <summary>
  /// Creates a new instance of <see cref="SmartTheoryAttribute"/> and constrains execution to a specific execution context
  /// </summary>
  /// <param name="execute"></param>
  public SmartTheoryAttribute(Execute execute)
  {
    Skip = TestExecutionResolver.Resolve(execute);
  }

  /// <summary>
  /// Creates a new instance of <see cref="SmartTheoryAttribute"/> and constrains execution to a specific execution context and platform
  /// </summary>
  /// <param name="execute"></param>
  /// <param name="on"></param>
  public SmartTheoryAttribute(Execute execute, On on)
  {
    Skip = TestExecutionResolver.Resolve(execute, on);
  }
}