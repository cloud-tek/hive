using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Hive.MicroServices.Demo.Aspire;

internal static class AspireExtensions
{
  public static IResourceBuilder<T> WithOtelCollector<T>(
    this IResourceBuilder<T> builder,
    IResourceBuilder<ContainerResource> otelCollector)
    where T : IResourceWithEnvironment, IResourceWithWaitSupport
  {
    return builder
      .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"))
      .WaitFor(otelCollector);
  }
}