namespace Ion.Logging;

public static class Startup
{
    public static IMicroService WithLogging(this IMicroService service, Action<LoggingConfigurationBuilder> log)
    {
        service.Extensions.Add(new Extension(service, log));

        return service;
    }
}