using Aspire.Hosting;

namespace Hive.MicroServices.Demo.Aspire;

internal static class AspireExtensions
{
  public static IResourceBuilder<T> WithOtelCollector<T>(
    this IResourceBuilder<T> builder,
    IResourceBuilder<ContainerResource> otelCollector)
    where T : IResourceWithEnvironment
  {
    return builder
      .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"));
  }
}
