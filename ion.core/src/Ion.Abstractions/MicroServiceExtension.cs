using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ion;

public abstract class MicroServiceExtension
{
    protected MicroServiceExtension(IMicroService service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IList<Action<IServiceCollection, IConfiguration>> ConfigureActions { get; } = new List<Action<IServiceCollection, IConfiguration>>();
    protected IConfigurationRoot Configuration
    {
        get;
        init;
    }

    protected IMicroService Service { get; init; }
    public virtual IApplicationBuilder Configure(IApplicationBuilder app, IMicroService microservice)
    {
        return app;
    }

    public virtual IApplicationBuilder ConfigureBeforeReadinessProbe(IApplicationBuilder app, IWebHostEnvironment env)
    {
        return app;
    }

    public virtual IEndpointRouteBuilder ConfigureEndpoints(IEndpointRouteBuilder builder)
    {
        return builder;
    }

    public virtual IHealthChecksBuilder ConfigureHealthChecks(IHealthChecksBuilder builder)
    {
        return builder;
    }

    public virtual IServiceCollection ConfigureServices(IServiceCollection services, IMicroService microservice)
    {
        return services;
    }
}