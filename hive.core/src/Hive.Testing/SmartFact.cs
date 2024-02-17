namespace Hive.Testing;

/// <summary>
/// Extends the XUnit FactAttribute to support conditional execution
/// </summary>
public sealed class SmartFactAttribute : Xunit.FactAttribute
{
  /// <summary>
  /// Creates a new instance of <see cref="SmartFactAttribute"/> and constrains execution to a specific platform
  /// </summary>
  /// <param name="on"></param>
  public SmartFactAttribute(On on)
  {
    Skip = TestExecutionResolver.Resolve(on);
  }

  /// <summary>
  /// Creates a new instance of <see cref="SmartFactAttribute"/> and constrains execution to a specific execution context
  /// </summary>
  /// <param name="execute"></param>
  public SmartFactAttribute(Execute execute)
  {
    Skip = TestExecutionResolver.Resolve(execute);
  }

  /// <summary>
  /// Creates a new instance of <see cref="SmartFactAttribute"/> and constrains execution to a specific execution context and platform
  /// </summary>
  /// <param name="execute"></param>
  /// <param name="on"></param>
  public SmartFactAttribute(Execute execute, On on)
  {
    Skip = TestExecutionResolver.Resolve(execute, on);
  }

  /// <summary>
  /// Creates a new instance of <see cref="SmartFactAttribute"/> and constrains execution to a specific execution context, platform and environment
  /// </summary>
  /// <param name="execute"></param>
  /// <param name="on"></param>
  /// <param name="environment"></param>
  public SmartFactAttribute(Execute execute, On on, params string[] environment)
  {
    Skip = TestExecutionResolver.Resolve(execute, on, environment);
  }
}