using Hive.Extensions;

namespace Hive.MicroServices;

public partial class MicroService
{
    public IList<MicroServiceExtension> Extensions { get; init; } = new List<MicroServiceExtension>();

    internal MicroService ConfigureExtensions()
    {
        this.Extensions.ForEach(extension =>
        {
            ConfigureActions.Add((services, configuration) => extension.ConfigureServices(services, this));
            ConfigurePipelineActions.Add((app) => extension.Configure(app, this));
            ConfigurePipelineActions.GetType();
        });

        return this;
    }
}
