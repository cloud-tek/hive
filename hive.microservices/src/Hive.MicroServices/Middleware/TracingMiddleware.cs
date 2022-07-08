using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Hive.MicroServices.Middleware;

public class TracingMiddleware
{
    private readonly RequestDelegate next;

    public TracingMiddleware(IMicroService service, RequestDelegate next)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(
                Constants.Headers.TraceParentId,
                out var requestId))
        {
            context.Request.Headers.TryGetValue(
                Constants.Headers.RequestId,
                out requestId);
        }

        var activity = new Activity(context.Request.Path);

        if (!string.IsNullOrEmpty(requestId))
        {
            activity.SetParentId(requestId);
        }

        activity.Start();
        try
        {
            await next.Invoke(context);
        }
        finally
        {
            activity.Stop();
        }
    }
}
