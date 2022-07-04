using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hive.Logging.LogzIo;

internal sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLoggingMiddleware> logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
            var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

            var statusCode = context.Response?.StatusCode;
            if (statusCode < 500)
            {
                logger.LogInformation("HTTP {Method} {Path} responded with {StatusCode} in {Elapsed:0.0000} [ms]",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs);
            }
            else
            {
                logger.LogError("HTTP {Method} {Path} responded with {StatusCode} in {Elapsed:0.0000} [ms]",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs);
            }
        }
        catch (Exception ex) when (LogException(context, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex))
        {
        }
    }

    private bool LogException(HttpContext context, double elapsedMs, Exception ex)
    {
        logger.LogError("HTTP {Method} {Path} failed with {StatusCode} in {Elapsed:0.0000} [ms]. {@Exception}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs, ex);

        return false;
    }

    private static double GetElapsedMilliseconds(long start, long stop)
    {
        return ((stop - start) * 1000 / (double)Stopwatch.Frequency);
    }
}