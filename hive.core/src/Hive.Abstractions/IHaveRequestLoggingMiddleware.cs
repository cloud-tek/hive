using Microsoft.AspNetCore.Builder;

namespace Hive;

public interface IHaveRequestLoggingMiddleware
{
    Action<IApplicationBuilder> ConfigureRequestLoggingMiddleware { get; }
}
