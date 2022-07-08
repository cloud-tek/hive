using Hive.Logging.AppInsights.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Hive.Logging.AppInsights;

public static class OptionsExtensions
{
    public static TelemetryConfiguration ToTelemetryConfiguration(this Options options)
    {
        return new TelemetryConfiguration()
        {
            InstrumentationKey = options.InstrumentationKey
        }.ConfigureSampling(options);
    }
}