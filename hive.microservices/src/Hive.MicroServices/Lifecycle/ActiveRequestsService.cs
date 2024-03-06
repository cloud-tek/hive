namespace Hive.MicroServices.Lifecycle;

internal class ActiveRequestsService : IActiveRequestsService
{
  private long counter;

  public long Counter => Interlocked.Read(ref counter);

  public ActiveRequestsService()
  {
  }

  public bool HasActiveRequests => Counter > 0;

  public void Decrement()
  {
    Interlocked.Decrement(ref counter);
  }

  public void Increment()
  {
    Interlocked.Increment(ref counter);
  }
}