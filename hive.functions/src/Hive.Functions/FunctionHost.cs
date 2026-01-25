using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.Functions;

/// <summary>
/// Implementation of Azure Functions host with Hive framework integration
/// </summary>
public class FunctionHost : IFunctionHost
{
  private readonly List<Action<IServiceCollection, IConfiguration>> configureActions = new();
  private readonly List<Action<IFunctionsWorkerApplicationBuilder>> functionConfigureActions = new();
  private IHost? host;

  /// <summary>
  /// Initializes a new instance of the FunctionHost class
  /// </summary>
  /// <param name="name">The name of the function host</param>
  public FunctionHost(string name)
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Id = Guid.NewGuid().ToString();
    Args = Array.Empty<string>();

    // Load environment variables
    var envVars = System.Environment.GetEnvironmentVariables();
    var envDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var key in envVars.Keys)
    {
      if (key is string keyStr)
      {
        var value = envVars[key]?.ToString();
        if (value != null)
        {
          envDict[keyStr] = value;
        }
      }
    }
    EnvironmentVariables = envDict;

    // Determine environment
    Environment = EnvironmentVariables.TryGetValue("AZURE_FUNCTIONS_ENVIRONMENT", out var funcEnv) && !string.IsNullOrWhiteSpace(funcEnv)
      ? funcEnv
      : EnvironmentVariables.TryGetValue("ASPNETCORE_ENVIRONMENT", out var aspEnv) && !string.IsNullOrWhiteSpace(aspEnv)
        ? aspEnv
        : "Production";

    CancellationTokenSource = new CancellationTokenSource();
    ConfigurationRoot = default!; // Will be set during CreateHostBuilder

    // Core configuration - register the FunctionHost instance
    configureActions.Add((services, config) =>
    {
      services.AddSingleton<IMicroServiceCore>(this);
      services.AddSingleton<IFunctionHost>(this);
      services.AddApplicationInsightsTelemetryWorkerService();
    });
  }

  /// <inheritdoc />
  public string Name { get; }

  /// <inheritdoc />
  public string Id { get; }

  /// <inheritdoc />
  public IConfigurationRoot ConfigurationRoot { get; private set; }

  /// <inheritdoc />
  public IList<MicroServiceExtension> Extensions { get; } = new List<MicroServiceExtension>();

  /// <inheritdoc />
  public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

  /// <inheritdoc />
  public string Environment { get; }

  /// <inheritdoc />
  public CancellationTokenSource CancellationTokenSource { get; }

  /// <inheritdoc />
  public string[] Args { get; }

  /// <inheritdoc />
  public bool ExternalLogger => false;

  /// <inheritdoc />
  public MicroServiceHostingMode HostingMode => MicroServiceHostingMode.Process;

  /// <inheritdoc />
  public IFunctionHost ConfigureServices(Action<IServiceCollection, IConfiguration> configure)
  {
    configureActions.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
    return this;
  }

  /// <inheritdoc />
  public IFunctionHost ConfigureFunctions(Action<IFunctionsWorkerApplicationBuilder> configure)
  {
    functionConfigureActions.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
    return this;
  }

  /// <inheritdoc />
  IMicroServiceCore IMicroServiceCore.RegisterExtension<TExtension>()
  {
    var extension = TExtension.Create(this);
    Extensions.Add(extension);

    // Extension participates in service configuration
    configureActions.Add((services, config) =>
    {
      extension.ConfigureServices(services, this);
    });

    return this;
  }

  /// <inheritdoc />
  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    host = CreateHostBuilder();
    await host.RunAsync(cancellationToken);
  }

  /// <inheritdoc />
  Task IMicroServiceCore.InitializeAsync(IConfigurationRoot? configuration, params string[] args)
  {
    host = CreateHostBuilder();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  Task IMicroServiceCore.StartAsync()
  {
    if (host == null)
      throw new InvalidOperationException("Host not initialized. Call InitializeAsync first.");
    return host.StartAsync(CancellationTokenSource.Token);
  }

  /// <inheritdoc />
  Task IMicroServiceCore.StopAsync()
  {
    if (host == null)
      return Task.CompletedTask;
    return host.StopAsync(CancellationTokenSource.Token);
  }

  private IHost CreateHostBuilder()
  {
    var builder = new HostBuilder();

    // Load configuration (same pattern as MicroService)
    builder.ConfigureAppConfiguration((context, config) =>
    {
      config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
      config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
      config.AddJsonFile("appsettings.shared.json", optional: true);
      config.AddEnvironmentVariables();
    });

    // Configure Azure Functions Worker
    builder.ConfigureFunctionsWorkerDefaults(app =>
    {
      // Apply all function-specific configuration
      foreach (var action in functionConfigureActions)
      {
        action(app);
      }
    });

    // Configure services (DI container)
    builder.ConfigureServices((context, services) =>
    {
      ConfigurationRoot = (IConfigurationRoot)context.Configuration;

      // Apply all service configuration actions
      foreach (var action in configureActions)
      {
        action(services, context.Configuration);
      }
    });

    return builder.Build();
  }

  /// <inheritdoc />
  public void Dispose()
  {
    host?.Dispose();
    CancellationTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    if (host != null)
    {
      await host.StopAsync(CancellationTokenSource.Token);
      host.Dispose();
    }
    CancellationTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }
}