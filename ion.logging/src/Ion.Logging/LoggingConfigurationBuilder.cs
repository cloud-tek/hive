using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ion.Logging;

public sealed class LoggingConfigurationBuilder
{
    internal readonly IList<Action<LoggerConfiguration, IServiceCollection, IMicroService>> Sinks = new List<Action<LoggerConfiguration, IServiceCollection, IMicroService>>();
    internal Extension Extension { get; }

    internal LoggingConfigurationBuilder(Extension extension)
    {
        this.Extension = extension;
    }

    public LoggingConfigurationBuilder ToConsole()
    {
        Sinks.Add((logger, _, _) => logger.WriteTo.Async(@async => @async.Console()));

        return this;
    }
}
