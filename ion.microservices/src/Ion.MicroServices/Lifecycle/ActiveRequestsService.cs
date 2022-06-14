namespace Ion.MicroServices.Lifecycle;

internal class ActiveRequestsService : IActiveRequestsService
{
    private long counter = 0L;

    public long Counter
    {
        get
        {
            return Interlocked.Read(ref counter);
        }
    }

    public ActiveRequestsService()
    {
    }

    public bool HasActiveRequests
    {
        get
        {
            return Counter > 0;
        }
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref counter);
    }

    public void Increment()
    {
        Interlocked.Increment(ref counter);
    }
}