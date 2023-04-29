namespace Hive.MicroServices.Job;

public class JobException : Exception
{
  public JobException(string message)
    : base(message)
  {
  }

  public JobException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
