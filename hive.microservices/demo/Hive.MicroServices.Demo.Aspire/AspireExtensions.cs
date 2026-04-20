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

  public static IResourceBuilder<T> WithRabbitMq<T>(
    this IResourceBuilder<T> builder,
    IResourceBuilder<ContainerResource> rabbitmq)
    where T : IResourceWithEnvironment, IResourceWithWaitSupport
  {
    return builder
      .WithEnvironment("Hive__Messaging__Transport", "RabbitMQ")
      .WithEnvironment("Hive__Messaging__RabbitMq__ConnectionUri",
        "amqp://guest:guest@localhost:5672")
      .WithEnvironment("Hive__Messaging__RabbitMq__AutoProvision", "true")
      .WaitFor(rabbitmq);
  }
}