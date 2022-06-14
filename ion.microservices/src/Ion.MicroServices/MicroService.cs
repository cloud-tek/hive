using System.Diagnostics;
using Ion.Exceptions;
using Ion.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;

namespace Ion.MicroServices;

public partial class MicroService : MicroServiceBase, IMicroService
{
    public MicroService(string name) : this(name, null)
    {
    }

    public MicroService(string name, ILogger<IMicroService> logger) : base()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));

        if (logger != null)
        {
            ExternalLogger = true;
            Logger = logger;
        }

        ConfigureActions.Add((svc, configuration) =>
        {
            svc.AddSingleton<IMicroService>(this);
            svc.AddAuthorization();
            svc.AddLogging(logger => logger.AddConsole());
            svc.Configure<HostOptions>(options => options.ShutdownTimeout = 60.Seconds());
        });
    }

    public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
    public IHost Host { get; private set; }

    public IMicroServiceLifetime Lifetime { get; } = new MicroServiceLifetime();
    public string Name { get; }
    public MicroServicePipelineMode PipelineMode { get; set; } = MicroServicePipelineMode.NotSet;
    public IServiceProvider ServiceProvider => Host.Services;

    public IConfigurationRoot ConfigurationRoot { get; private set; }

    internal List<Action<IServiceCollection, IConfiguration>> ConfigureActions { get; } = new List<Action<IServiceCollection, IConfiguration>>();

    internal List<Action<IApplicationBuilder>> ConfigurePipelineActions { get; } = new List<Action<IApplicationBuilder>>();

    internal Func<Assembly> MicroServiceEntrypointAssemblyProvider { get; set; } = () => Assembly.GetEntryAssembly();
    
    public Task InitializeAsync(IConfigurationRoot configuration = null, params string[] args)
    {
        System.Diagnostics.Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        Host = CreateHostBuilder(configuration, args);
        
        return Task.CompletedTask;
    }

    public IMicroService RegisterExtension<TExtension>()
                            where TExtension : MicroServiceExtension, new()
    {
        Extensions.Add(new TExtension());

        return this;
    }

    public async Task RunAsync(IConfigurationRoot configuration = null, params string[] args)
    {
        await InitializeAsync(configuration, args).ConfigureAwait(false);

        try
        {
            if (PipelineMode == MicroServicePipelineMode.NotSet)
            {
                throw new ConfigurationException(Constants.Errors.PipelineNotSet);
            }

            await Host.RunAsync(CancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Unhandled exception in {Service}: {@Exception}", Name, ex);
            throw;
        }
    }

    private IHost CreateHostBuilder(IConfigurationRoot configuration = null, params string[] args)
    {
        var host = global::Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseConsoleLifetime()
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                if (configuration != null)
                {
                    cfg.AddConfiguration(configuration);
                }
                else
                {
                    cfg
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile($"appsettings.{Environment}.json", optional: true)
                        .AddJsonFile("shared.json", optional: true)
                        .AddJsonFile($"shared.{Environment}.json", optional: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                }

                ConfigurationRoot = cfg.Build();
            })
            // .ConfigureLogging((logging) =>
            // {
            //     logging.AddConsole();
            // })
            .ConfigureWebHostDefaults(app =>
            {
                // (!) Important, .UseSettings MUST be called AFTER Configure
                // See: https://github.com/dotnet/aspnetcore/issues/38672
                app
                    .ConfigureServices((ctx, services) =>
                    {
                        services.AddSingleton<IConfigurationRoot>(ConfigurationRoot);
                        services.AddSingleton<IConfiguration>(ConfigurationRoot);

                        ConfigureActions.ForEach(action => action(services, ConfigurationRoot));

                        Extensions.ForEach(extension => extension.ConfigureActions.ForEach(action => action(services, ConfigurationRoot)));
                    })
                    .Configure(app =>
                    {
                        /* TODO:
                         var listener = new TestDiagnosticListener();
                         diagnosticListener.SubscribeWithAdapter(listener);
                         */

                        var lex = Extensions.SingleOrDefault(ex => ex is IHaveRequestLoggingMiddleware) as IHaveRequestLoggingMiddleware;
                        if (lex != null && lex.ConfigureRequestLoggingMiddleware != null)
                        {
                            lex.ConfigureRequestLoggingMiddleware(app);
                        }
                        
                        ConfigurePipelineActions.ForEach(action => action(app));
                    })
                    .UseSetting(WebHostDefaults.ApplicationKey, MicroServiceEntrypointAssemblyProvider().FullName);
            })
            .Build();

        return host;
    }
}