using System.Diagnostics;
using System.Reflection;
using Hive.Configuration;
using Hive.Exceptions;
using Hive.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.MicroServices;

/// <summary>
/// The microservice class, containing the initialization and runtime logic for all microservices using it.
/// </summary>
public partial class MicroService : MicroServiceBase, IMicroService
{
  /// <summary>
  /// Creates a new <see cref="MicroService"/> instance
  /// </summary>
  /// <param name="name"></param>
  public MicroService(string name) : this(name, null)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="MicroService"/> class.
  /// </summary>
  /// <param name="name"></param>
  /// <param name="logger"></param>
  public MicroService(string name, ILogger<IMicroService>? logger) : base()
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));

    if (logger != null)
    {
      ExternalLogger = true;
      Logger = logger;
    }

    // Eagerly initialize ConfigurationRoot to eliminate temporal coupling
    // This ensures ConfigurationRoot is always available, even before InitializeAsync
    ConfigurationRoot = ConfigurationBuilderFactory.CreateDefault(Environment).Build();

    ConfigureActions.Add((svc, configuration) =>
    {
      svc.AddSingleton<IMicroServiceCore>(this);
      svc.AddSingleton<IMicroService>(this);
      svc.AddAuthorization();
      svc.AddLogging(logger => logger.AddConsole());
      svc.Configure<HostOptions>(options => options.ShutdownTimeout = 60.Seconds());
    });
  }

  /// <summary>
  /// The cancellation token source for the microservice. This can be used to cancel the microservice's run.
  /// </summary>
  public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

  /// <summary>
  /// The the microservice's <see cref="IHost"/>.
  /// </summary>
  public IHost Host { get; private set; } = default!;

  /// <summary>
  /// The microservice's <see cref="IMicroServiceLifetime"/>
  /// </summary>
  public IMicroServiceLifetime Lifetime { get; } = new MicroServiceLifetime();

  /// <summary>
  /// The microservice's name.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The microservice's <see cref="MicroServicePipelineMode"/>
  /// </summary>
  public MicroServicePipelineMode PipelineMode { get; set; } = MicroServicePipelineMode.NotSet;

  /// <summary>
  /// The microservice's <see cref="IServiceProvider"/>
  /// </summary>
  public IServiceProvider ServiceProvider => Host.Services;

  /// <summary>
  /// The microservice's <see cref="IConfigurationRoot"/>
  /// </summary>
  public IConfigurationRoot ConfigurationRoot { get; private set; } = default!;

  /// <summary>
  /// The microservice's commandline args
  /// </summary>
  public string[] Args { get; private set; } = default!;

  /// <summary>
  /// The microservice's list of actions executed during the configuration phase against the <see cref="IServiceCollection"/> and <see cref="IConfiguration"/>.
  /// </summary>
  internal List<Action<IServiceCollection, IConfiguration>> ConfigureActions { get; } = new List<Action<IServiceCollection, IConfiguration>>();

  /// <summary>
  /// The microservice's list of actions executed during the pipeline configuration phase against the <see cref="IApplicationBuilder"/>
  /// </summary>
  internal List<Action<IApplicationBuilder>> ConfigurePipelineActions { get; } = new List<Action<IApplicationBuilder>>();

  internal Func<Assembly> MicroServiceEntrypointAssemblyProvider { get; set; } = () => Assembly.GetEntryAssembly()!;

  /// <summary>
  /// Optional external host builder factory for test scenarios.
  /// When set, InitializeAsync will call this factory with configuration to build the host.
  /// </summary>
  internal Func<IConfigurationRoot, IHost>? ExternalHostFactory { get; set; }

  /// <summary>
  /// Initializes the microservice and creates the host but does not start it.
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="args"></param>
  /// <returns><see cref="Task"/></returns>
  public Task InitializeAsync(IConfigurationRoot? configuration = null, params string[] args)
  {
    Activity.DefaultIdFormat = ActivityIdFormat.W3C;

    if (ExternalHostFactory != null)
    {
      var config = configuration ?? new ConfigurationBuilder().Build();
      Host = ExternalHostFactory(config);
      // Replace eagerly-initialized configuration with the provided or default configuration
      ConfigurationRoot = config;
    }
    else
    {
      Host = CreateHostBuilder(configuration, args);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Registers an extension with the microservice.
  /// Uses compile-time enforced factory method from IMicroServiceExtension.
  /// </summary>
  /// <typeparam name="TExtension">
  /// Type of the extension.
  /// Must implement IMicroServiceExtension&lt;TExtension&gt; with a Create factory method.
  /// </typeparam>
  /// <returns><see cref="IMicroService"/></returns>
  public IMicroService RegisterExtension<TExtension>()
    where TExtension : MicroServiceExtension<TExtension>, IMicroServiceExtension<TExtension>
  {
    var extension = TExtension.Create(this);
    Extensions.Add(extension);

    return this;
  }

  /// <summary>
  /// Explicit interface implementation for IMicroServiceCore.RegisterExtension
  /// </summary>
  IMicroServiceCore IMicroServiceCore.RegisterExtension<TExtension>()
  {
    return RegisterExtension<TExtension>();
  }

  /// <summary>
  /// Initializes and runs the microservice.
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="args"></param>
  /// <returns><see cref="Task"/> returning an exit code</returns>
  /// <exception cref="ConfigurationException">Thrown when configuration validation fails</exception>
  /// <remarks>
  /// This method is not thread-safe and must not be called concurrently.
  /// It is designed to be invoked once per application lifetime at startup.
  /// For scenarios requiring separate initialization and execution phases,
  /// use <see cref="InitializeAsync"/> followed by <see cref="IMicroServiceCore.StartAsync"/> instead.
  /// </remarks>
  public async Task<int> RunAsync(IConfigurationRoot? configuration = null, params string[] args)
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
      Logger.LogUnhandledException(Name, ex);
      return -1;
    }

    return 0;
  }

  /// <summary>
  /// Starts the microservice after initialization. Returns immediately after starting.
  /// </summary>
  /// <returns><see cref="Task"/></returns>
  /// <exception cref="ConfigurationException">Thrown when configuration validation fails</exception>
  public async Task StartAsync()
  {
    try
    {
      if (PipelineMode == MicroServicePipelineMode.NotSet)
      {
        throw new ConfigurationException(Constants.Errors.PipelineNotSet);
      }

      await Host.StartAsync(CancellationTokenSource.Token);
    }
    catch (Exception ex)
    {
      Logger.LogUnhandledException(Name, ex);
      throw;
    }
  }

  /// <summary>
  /// Stops the microservice.
  /// </summary>
  /// <returns><see cref="Task"/></returns>
  public async Task StopAsync()
  {
    try
    {
      await Host.StopAsync(CancellationTokenSource.Token);
    }
    catch (Exception ex)
    {
      Logger.LogUnhandledException(Name, ex);
      throw;
    }
  }

  /// <summary>
  /// Asynchronously disposes the microservice, releasing all managed resources including the host and cancellation token source.
  /// </summary>
  /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation</returns>
  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);
    Dispose(disposing: false);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Synchronously disposes the microservice, releasing all managed resources including the host and cancellation token source.
  /// </summary>
  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Core async disposal logic for releasing managed resources.
  /// </summary>
  /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation</returns>
  protected virtual ValueTask DisposeAsyncCore()
  {
    // IHost only implements IDisposable, not IAsyncDisposable
    // Dispose it synchronously
    Host?.Dispose();

    // Dispose CancellationTokenSource synchronously (only supports IDisposable)
    CancellationTokenSource?.Dispose();

    // Dispose Lifetime and its internal CancellationTokenSources
    Lifetime?.Dispose();

    return ValueTask.CompletedTask;
  }

  /// <summary>
  /// Synchronous disposal logic for releasing managed resources.
  /// </summary>
  /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
  protected virtual void Dispose(bool disposing)
  {
    if (disposing)
    {
      // Dispose IHost synchronously if it supports IDisposable
      if (Host is IDisposable disposable)
      {
        disposable.Dispose();
      }

      // Dispose CancellationTokenSource
      CancellationTokenSource?.Dispose();

      // Dispose Lifetime and its internal CancellationTokenSources
      Lifetime?.Dispose();
    }
  }

  private IHost CreateHostBuilder(IConfigurationRoot? configuration = null, params string[] args)
  {
    Args = args;
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
              .AddJsonFile("appsettings.json", optional: true)
              .AddJsonFile($"appsettings.{Environment}.json", optional: true)
              .AddSharedConfiguration(Environment)
              .AddEnvironmentVariables()
              .AddCommandLine(args);
          }

          // Replace eagerly-initialized configuration with the host's built configuration
          ConfigurationRoot = cfg.Build();
        })
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
              ConfigurePipelineActions.ForEach(action => action(app));
            })
            .UseSetting(WebHostDefaults.ApplicationKey, MicroServiceEntrypointAssemblyProvider().FullName);
        })
        .Build();

    return host;
  }
}