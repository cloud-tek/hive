using System.Diagnostics;
using System.Net.Http;
using CloudTek.Testing;
using FluentAssertions;
using Hive.MicroServices.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Xunit;

namespace Hive.OpenTelemetry.Tests.E2E;

/// <summary>
/// Pins the documented customisation recipe: SDK provider configuration added next to
/// WithOpenTelemetry() adds to the defaults and never replaces them.
/// </summary>
[Collection("E2E Tests")]
public class AdditiveProviderConfigurationTests : E2ETestBase
{
  private static readonly ActivitySource CustomSource = new("Hive.OpenTelemetry.Tests.Additive");

  [Fact]
  [IntegrationTest]
  public async Task WithOpenTelemetry_with_additive_tracer_provider_configuration_exports_custom_activity_source()
  {
    // Arrange
    var service = CreateTracingTestService<AdditiveProviderConfigurationTests>(
      "additive-provider-tests",
      app => app.MapGet("/custom", () =>
      {
        using var activity = CustomSource.StartActivity("custom-op");
        return "OK";
      }));

    service.ConfigureServices((svc, _) =>
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) => builder.AddSource(CustomSource.Name)));

    // Act
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/custom");
    });

    // Assert
    ExportedActivities.Should().Contain(
      a => a.Source.Name == CustomSource.Name,
      "the additive AddSource registration should export the custom activity source");
    ExportedActivities.Should().Contain(
      a => a.Kind == ActivityKind.Server,
      "the default ASP.NET Core instrumentation must not be replaced");
  }
}