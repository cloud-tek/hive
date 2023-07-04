using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Hive;

public abstract class MicroServiceBase
{
    private readonly ConcurrentDictionary<string, bool> microserviceFlags = new ConcurrentDictionary<string, bool>();
    protected MicroServiceBase()
    {
        EnvironmentVariables = new ReadOnlyDictionary<string, string>(
           global::System.Environment.GetEnvironmentVariables()
               .OfType<DictionaryEntry>()
               .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!));

        if (EnvironmentVariables.Any(variable => variable.Key.StartsWith(Constants.EnvironmentVariables.Kubernetes.KubernetesVariablePrefix)))
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

    public ILogger<IMicroService> Logger { get; set; } = default!;
    public string Environment { get; } = global::System.Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.DotNet.Environment)?.ToLower() ?? "dev";
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; private set; }

    /// <summary>
    /// Flag indicating whether the IMicroservice's logger is provided externally
    /// </summary>
    public bool ExternalLogger { get; protected init; } = false;
    public MicroServiceHostingMode HostingMode { get; init; }

    public string HostName { get; } = global::System.Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.Hostname) ?? "localhost";
    public string Id { get; } = System.Guid.NewGuid().ToString();

    /// <summary>
    /// Flag indicating whether the IMicroservice is ready to receive traffic
    /// </summary>
    public bool IsReady
    {
        get => IsStarted && this.microserviceFlags[nameof(IsReady)];

        set => this.microserviceFlags[nameof(IsReady)] = value;
    }

    /// <summary>
    /// Flag indicating whether the IMicroservice has completed it's startup cycle
    /// </summary>
    public bool IsStarted
    {
        get => this.microserviceFlags[nameof(IsStarted)];

        set
        {
            this.microserviceFlags[nameof(IsStarted)] = value;

            //if (value == true) ((MicroServiceLifetime)this.Lifetime).ServiceStartedTokenSource.Cancel();
        }
    }

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
}
