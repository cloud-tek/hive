using Ion.Logging.AppInsights.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Ion.Logging.AppInsights;

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