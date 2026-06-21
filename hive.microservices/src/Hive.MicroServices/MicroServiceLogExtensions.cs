using Microsoft.Extensions.Logging;

namespace Hive.MicroServices;

internal static partial class MicroServiceLogExtensions
{
  [LoggerMessage((int)MicroServiceLogEventId.EnvironmentVariableConflict, LogLevel.Critical, "Environment variable conflict detected: {Message}")]
  internal static partial void LogEnvironmentVariableConflict(this ILogger logger, string message);

  [LoggerMessage((int)MicroServiceLogEventId.UnhandledException, LogLevel.Critical, "Unhandled exception in {Service}")]
  internal static partial void LogUnhandledException(this ILogger logger, string service, Exception ex);
}