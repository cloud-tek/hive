using Hive.Extensions;

namespace Hive.MicroServices;

public partial class MicroService
{
  /// <summary>
  /// The extensions for a microservice.
  /// </summary>
  public IList<MicroServiceExtension> Extensions { get; init; } = new List<MicroServiceExtension>();

  internal MicroService ConfigureExtensions()
  {
    Extensions.ForEach(extension =>
    {
      ConfigureActions.Add((services, configuration) => extension.ConfigureServices(services, this));
      ConfigurePipelineActions.Add((app) => extension.Configure(app, this));
    });

    return this;
  }
}