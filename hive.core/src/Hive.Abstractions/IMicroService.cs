using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hive;

public interface IMicroService
{
    CancellationTokenSource CancellationTokenSource { get; }
    IConfigurationRoot ConfigurationRoot { get; }
    string Environment { get; }

    string[] Args { get; }
    IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
    IList<MicroServiceExtension> Extensions { get; }

    bool ExternalLogger { get; }
    MicroServiceHostingMode HostingMode { get; }
    string Id { get; }
    bool IsReady { get; }
    bool IsStarted { get; }
    IMicroServiceLifetime Lifetime { get; }
    string Name { get; }
    MicroServicePipelineMode PipelineMode { get; }

    IMicroService RegisterExtension<TExtension>() where TExtension : MicroServiceExtension, new();
    Task<int> RunAsync(IConfigurationRoot configuration = default!, params string[] args);
}
