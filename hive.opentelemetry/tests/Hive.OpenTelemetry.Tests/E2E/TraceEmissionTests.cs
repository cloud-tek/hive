using System.Diagnostics;
using System.Net.Http;
using FluentAssertions;
using Hive.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Hive.OpenTelemetry.Tests.E2E;

/// <summary>
/// End-to-end tests for trace emission via OpenTelemetry instrumentation
/// </summary>
[Collection("E2E Tests")]
public class TraceEmissionTests : E2ETestBase
{
  private const string ServiceName = "trace-emission-tests";

  [Fact]
  [IntegrationTest]
  public async Task GivenApiPipeline_WhenHttpRequestMade_ThenTraceIsEmitted()
  {
    // Arrange
    var service = CreateTracingTestService<TraceEmissionTests>(
      ServiceName,
      app => app.MapGet("/test", () => "OK"));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      var response = await client.GetAsync("/test");
      response.EnsureSuccessStatusCode();
    });

    // Assert - verify trace was captured
    ExportedActivities.Should().NotBeEmpty("traces should be captured for HTTP requests");
    ExportedActivities.Should().Contain(a =>
      a.DisplayName.Contains("GET") || a.OperationName.Contains("GET"),
      "should contain a GET request trace");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenApiPipeline_WhenMultipleRequestsMade_ThenMultipleTracesAreEmitted()
  {
    // Arrange
    var service = CreateTracingTestService<TraceEmissionTests>(
      ServiceName,
      app =>
      {
        app.MapGet("/endpoint1", () => "Response 1");
        app.MapGet("/endpoint2", () => "Response 2");
      });

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();

      await client.GetAsync("/endpoint1");
      await client.GetAsync("/endpoint2");
      await client.GetAsync("/endpoint1");
    });

    // Assert - verify multiple traces were captured
    ExportedActivities.Should().HaveCountGreaterOrEqualTo(3,
      "each HTTP request should generate a trace");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenApiPipeline_WhenRequestMade_ThenTraceContainsHttpAttributes()
  {
    // Arrange
    var service = CreateTracingTestService<TraceEmissionTests>(
      ServiceName,
      app => app.MapGet("/attributes-test", () => "OK"));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/attributes-test");
    });

    // Assert - verify trace has HTTP semantic convention attributes
    var httpActivity = ExportedActivities.FirstOrDefault(a =>
      a.DisplayName.Contains("GET") || a.OperationName.Contains("GET"));

    httpActivity.Should().NotBeNull("an HTTP trace should be captured");

    // Check for common HTTP attributes
    var tags = httpActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);

    // ASP.NET Core instrumentation adds http.request.method or http.method
    var hasHttpMethod = tags.ContainsKey("http.request.method") ||
                        tags.ContainsKey("http.method");
    hasHttpMethod.Should().BeTrue("trace should contain HTTP method attribute");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenApiPipeline_WhenPostRequestMade_ThenTraceReflectsPostMethod()
  {
    // Arrange
    var service = CreateTracingTestService<TraceEmissionTests>(
      ServiceName,
      app => app.MapPost("/post-test", () => "Created"));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.PostAsync("/post-test", new StringContent(""));
    });

    // Assert
    ExportedActivities.Should().Contain(a =>
      a.DisplayName.Contains("POST") || a.OperationName.Contains("POST"),
      "should contain a POST request trace");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenApiPipeline_WhenRequestReturnsError_ThenTraceReflectsErrorStatus()
  {
    // Arrange
    var service = CreateTracingTestService<TraceEmissionTests>(
      ServiceName,
      app => app.MapGet("/error-test", () => Results.StatusCode(500)));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      var response = await client.GetAsync("/error-test");
      // Don't throw on 500 - we want to verify the trace
    });

    // Assert - verify trace reflects error
    var errorActivity = ExportedActivities.FirstOrDefault(a =>
      a.DisplayName.Contains("GET") || a.OperationName.Contains("GET"));

    errorActivity.Should().NotBeNull("an HTTP trace should be captured");

    // Check for error status or status code tag
    var tags = errorActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);
    var hasStatusCode = tags.TryGetValue("http.response.status_code", out var statusCode) ||
                        tags.TryGetValue("http.status_code", out statusCode);

    if (hasStatusCode)
    {
      statusCode.Should().Be("500", "trace should reflect the 500 status code");
    }
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenApiPipeline_WhenRequestMade_ThenTraceHasValidTraceId()
  {
    // Arrange
    var service = CreateTracingTestService<TraceEmissionTests>(
      ServiceName,
      app => app.MapGet("/traceid-test", () => "OK"));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/traceid-test");
    });

    // Assert - verify trace has valid TraceId
    var activity = ExportedActivities.FirstOrDefault();
    activity.Should().NotBeNull("a trace should be captured");

    activity!.TraceId.Should().NotBe(default(ActivityTraceId),
      "trace should have a valid TraceId");
    activity.SpanId.Should().NotBe(default(ActivitySpanId),
      "trace should have a valid SpanId");
  }
}