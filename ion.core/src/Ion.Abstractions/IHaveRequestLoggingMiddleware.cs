using Microsoft.AspNetCore.Builder;

namespace Ion;

public interface IHaveRequestLoggingMiddleware
{
    Action<IApplicationBuilder> ConfigureRequestLoggingMiddleware { get; }
}
