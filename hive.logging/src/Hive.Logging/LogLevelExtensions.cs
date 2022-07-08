using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Hive.Logging;

internal static class LogLevelExtensions
{
    private static readonly IDictionary<LogLevel, LogEventLevel> LogLevelMappings = new Dictionary<LogLevel, LogEventLevel>()
    {
        { LogLevel.Trace, LogEventLevel.Verbose },
        { LogLevel.Debug, LogEventLevel.Debug },
        { LogLevel.Information, LogEventLevel.Information },
        { LogLevel.Warning, LogEventLevel.Warning },
        { LogLevel.Error, LogEventLevel.Error },
        { LogLevel.Critical, LogEventLevel.Fatal }
    };

    internal static LogEventLevel ToSerilogLogLevel(this LogLevel level)
    {
        return LogLevelMappings[level];
    }
}
