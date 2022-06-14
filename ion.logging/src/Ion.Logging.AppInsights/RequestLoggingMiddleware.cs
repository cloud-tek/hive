using System.Diagnostics;
using Ion.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Ion.Logging.AppInsights;

public class RequestLoggingMiddleware : IMiddleware
{
    private readonly TelemetryClient client;

    public RequestLoggingMiddleware(Options options)
    {
        client = options.InstrumentationKey.IsNotNullOrEmpty()
            ? new TelemetryClient(options.ToTelemetryConfiguration())
            : new TelemetryClient(TelemetryConfiguration.CreateDefault());
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var start = DateTimeOffset.UtcNow;
        var startTicks = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            // _client.Context.User.Id = "";
            // _client.Context.Session.Id = "";
            // _client.Context.Operation.Id = "";

            var elapsedMs = GetElapsedMilliseconds(startTicks, Stopwatch.GetTimestamp());
            
            var telemetry = new RequestTelemetry()
            {
                Name = context.Request.Path.Value ?? "unknown",
                Timestamp = start,
                Duration = TimeSpan.FromMilliseconds(elapsedMs),
                ResponseCode = context.Response.StatusCode.ToString(),
                Success = context.Response.StatusCode < 400
            };
            
            telemetry.Properties.Add("Method", context.Request.Method);

            client.TrackRequest(telemetry);
        }
    }

    private static double GetElapsedMilliseconds(long start, long stop)
    {
        return ((stop - start) * 1000 / (double)Stopwatch.Frequency);
    }
}