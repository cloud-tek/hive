using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Hive;

/// <summary>
/// Base class for all microservices
/// </summary>
public abstract class MicroServiceBase
{
  private readonly ConcurrentDictionary<string, bool> _microserviceFlags = new ConcurrentDictionary<string, bool>();

  /// <summary>
  /// Default constructor
  /// </summary>
  protected MicroServiceBase()
  {
    EnvironmentVariables = new ReadOnlyDictionary<string, string>(
       global::System.Environment.GetEnvironmentVariables()
           .OfType<DictionaryEntry>()
           .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!));

    if (EnvironmentVariables.Any(variable => variable.Key.StartsWith(Constants.EnvironmentVariables.Kubernetes.KubernetesVariablePrefix, StringComparison.OrdinalIgnoreCase)))
    {
      HostingMode = MicroServiceHostingMode.Kubernetes;
    }
    else if (EnvironmentVariables.ContainsKey(Constants.EnvironmentVariables.DotNet.DotNetRunningInContainer))
    {
      HostingMode = MicroServiceHostingMode.Container;
    }
    else
    {
      HostingMode = MicroServiceHostingMode.Process;
    }

    IsStarted = false;
    IsReady = false;
  }

  /// <summary>
  /// The IMicroservice's logger
  /// </summary>
  public ILogger<IMicroService> Logger { get; set; } = default!;

  /// <summary>
  /// The IMicroservice's environment
  /// </summary>
  public string Environment { get; } = global::System.Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.DotNet.Environment)?.ToLower(CultureInfo.InvariantCulture) ?? "dev";

  /// <summary>
  /// The IMicroservice's command environment
  /// </summary>
  public IReadOnlyDictionary<string, string> EnvironmentVariables { get; private set; }

  /// <summary>
  /// Flag indicating whether the IMicroservice's logger is provided externally
  /// </summary>
  public bool ExternalLogger { get; protected init; }

  /// <summary>
  /// The IMicroservice's hosting mode
  /// </summary>
  public MicroServiceHostingMode HostingMode { get; init; }

  /// <summary>
  /// The IMicroservice's hostname
  /// </summary>
  public string HostName { get; } = global::System.Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.Hostname) ?? "localhost";

  /// <summary>
  /// The IMicroservice's Id
  /// </summary>
  public string Id { get; } = System.Guid.NewGuid().ToString();

  /// <summary>
  /// Flag indicating whether the IMicroservice is ready to receive traffic
  /// </summary>
  public bool IsReady
  {
    get => IsStarted && _microserviceFlags[nameof(IsReady)];

    set => _microserviceFlags[nameof(IsReady)] = value;
  }

  /// <summary>
  /// Flag indicating whether the IMicroservice has completed it's startup cycle
  /// </summary>
  public bool IsStarted
  {
    get => _microserviceFlags[nameof(IsStarted)];

    set => _microserviceFlags[nameof(IsStarted)] = value;
    // todo: verify if IMicroServiceLifetime should be implemented by MicroServiceBase
    // if (value == true)
    //   ((IMicroServiceLifetime)this.Lifetime).ServiceStartedTokenSource.Cancel();
  }

#pragma warning disable CA1822
  /// <summary>
  /// The IMicroservice's OS platform
  /// </summary>
  public OSPlatform Platform
  {
    get
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        return OSPlatform.Linux;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return OSPlatform.OSX;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return OSPlatform.Windows;

      throw new NotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
    }
  }
#pragma warning restore CA1822
}