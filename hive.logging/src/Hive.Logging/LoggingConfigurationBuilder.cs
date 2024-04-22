using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Hive.Logging;

/// <summary>
///  Provides a simple way to configure logging.
/// </summary>
public sealed class LoggingConfigurationBuilder
{
  internal readonly IList<Action<LoggerConfiguration, IServiceCollection, IMicroService>> Sinks = new List<Action<LoggerConfiguration, IServiceCollection, IMicroService>>();
  internal Extension Extension { get; }

  internal LoggingConfigurationBuilder(Extension extension)
  {
    Extension = extension;
  }

  /// <summary>
  /// Configures the logger to write to the console.
  /// </summary>
  /// <returns><see cref="LoggingConfigurationBuilder"/></returns>
  public LoggingConfigurationBuilder ToConsole()
  {
    Sinks.Add((logger, _, _) => logger.WriteTo.Async(@async => @async.Console(formatProvider: CultureInfo.InvariantCulture)));

    return this;
  }
}