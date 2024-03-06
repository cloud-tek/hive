namespace Hive.MicroServices.Job;

/// <summary>
/// An exception thrown by a job
/// </summary>
public class JobException : Exception
{
  /// <summary>
  /// Creates a new <see cref="JobException"/> instance
  /// </summary>
  /// <param name="message"></param>
  public JobException(string message)
    : base(message)
  {
  }

  /// <summary>
  /// Creates a new <see cref="JobException"/> instance
  /// </summary>
  /// <param name="message"></param>
  /// <param name="innerException"></param>
  public JobException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}