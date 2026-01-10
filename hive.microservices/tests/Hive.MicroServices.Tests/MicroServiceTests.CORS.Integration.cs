using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Hive.MicroServices.Api;
using Hive.MicroServices.Testing;
using Hive.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class CORSIntegration
  {
    private const string ServiceName = "microservice-cors-integration-tests";
    private readonly ITestOutputHelper _output;

    public CORSIntegration(ITestOutputHelper output)
    {
      _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    #region Helper Methods

    private async Task<(IMicroService service, HttpClient client)> CreateTestServiceWithCORS(
      params string[] configFiles)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORSIntegration).Assembly, "Hive.MicroServices.Tests", configFiles)
        .Build();

      var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .WithCORS()
        .ConfigureApiPipeline(endpoints =>
        {
          endpoints.MapGet("/api/test", () => Results.Ok(new { message = "Test endpoint" }));
          endpoints.MapPost("/api/test", () => Results.Ok(new { message = "POST received" }));
          endpoints.MapPut("/api/test", () => Results.Ok(new { message = "PUT received" }));
          endpoints.MapDelete("/api/test", () => Results.Ok(new { message = "DELETE received" }));
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      var server = ((MicroService)microservice).Host.GetTestServer();
      var client = server.CreateClient();

      return (microservice, client);
    }

    private HttpRequestMessage CreateSimpleRequest(string url, HttpMethod method, string origin)
    {
      var request = new HttpRequestMessage(method, url);
      request.Headers.Add("Origin", origin);
      return request;
    }

    private HttpRequestMessage CreatePreflightRequest(
      string url,
      string origin,
      string method,
      params string[] headers)
    {
      var request = new HttpRequestMessage(HttpMethod.Options, url);
      request.Headers.Add("Origin", origin);
      request.Headers.Add("Access-Control-Request-Method", method);

      if (headers.Length > 0)
      {
        request.Headers.Add("Access-Control-Request-Headers", string.Join(", ", headers));
      }

      return request;
    }

    private void AssertCorsHeaderPresent(HttpResponseMessage response, string headerName)
    {
      response.Headers.Should().Contain(h => h.Key == headerName,
        $"CORS header '{headerName}' should be present");
    }

    private void AssertCorsHeaderAbsent(HttpResponseMessage response, string headerName)
    {
      response.Headers.Should().NotContain(h => h.Key == headerName,
        $"CORS header '{headerName}' should NOT be present");
    }

    private void AssertCorsHeaderValue(HttpResponseMessage response, string headerName, string expectedValue)
    {
      response.Headers.GetValues(headerName)
        .Should().ContainSingle()
        .Which.Should().Be(expectedValue, $"CORS header '{headerName}' should have value '{expectedValue}'");
    }

    #endregion

    #region Scenario 1: AllowAny Mode

    [Fact]
    [IntegrationTest]
    public async Task Scenario1_AllowAny_SimpleGetRequest_ReturnsCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-allowany.json");

      try
      {
        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://example.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderValue(response, "Access-Control-Allow-Origin", "*");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario1_AllowAny_PreflightRequest_ReturnsCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-allowany.json");

      try
      {
        var request = CreatePreflightRequest("/api/test", "https://example.com", "POST", "Content-Type");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderValue(response, "Access-Control-Allow-Origin", "*");
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Methods");
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Headers");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario1_AllowAny_AnyOriginAllowed_ReturnsCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-allowany.json");

      try
      {
        // Test with "malicious" origin - should still work with AllowAny
        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://malicious.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderValue(response, "Access-Control-Allow-Origin", "*");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    #endregion

    #region Scenario 2: Named Policy - Restrictive Origin

    [Fact]
    [IntegrationTest]
    public async Task Scenario2_RestrictivePolicy_AllowedOrigin_ReturnsCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://trusted.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderValue(response, "Access-Control-Allow-Origin", "https://trusted.com");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario2_RestrictivePolicy_DisallowedOrigin_NoCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://untrusted.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Server still responds
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Origin"); // But NO CORS headers
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario2_RestrictivePolicy_AllowedOrigin_PreflightSucceeds()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreatePreflightRequest("/api/test", "https://trusted.com", "POST", "Content-Type");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderValue(response, "Access-Control-Allow-Origin", "https://trusted.com");
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Methods");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario2_RestrictivePolicy_DisallowedOrigin_PreflightFails()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreatePreflightRequest("/api/test", "https://untrusted.com", "POST", "Content-Type");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // Preflight may return 204/200 but without CORS headers, browser will block it
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Origin");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    #endregion

    #region Scenario 3: Method Restrictions

    [Fact]
    [IntegrationTest]
    public async Task Scenario3_MethodRestrictions_AllowedMethod_Get_ReturnsCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://trusted.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario3_MethodRestrictions_AllowedMethod_Post_ReturnsCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreateSimpleRequest("/api/test", HttpMethod.Post, "https://trusted.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario3_MethodRestrictions_DisallowedMethod_Preflight_MethodNotInAllowedMethods()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreatePreflightRequest("/api/test", "https://trusted.com", "DELETE");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // Even if preflight returns headers, DELETE should not be in allowed methods
        if (response.Headers.Contains("Access-Control-Allow-Methods"))
        {
          var allowedMethods = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods"));
          allowedMethods.Should().NotContain("DELETE", "DELETE method should not be in allowed methods");
        }
      }
      finally
      {
        await service.StopAsync();
      }
    }

    #endregion

    #region Scenario 4: Header Restrictions

    [Fact]
    [IntegrationTest]
    public async Task Scenario4_HeaderRestrictions_AllowedHeaders_Preflight_ReturnsCorrectHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-restrictive.json");

      try
      {
        var request = CreatePreflightRequest("/api/test", "https://trusted.com", "POST", "Content-Type", "X-Custom-Header");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Headers");

        var allowedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        allowedHeaders.Should().Contain("Content-Type");
        allowedHeaders.Should().Contain("X-Custom-Header");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    #endregion

    #region Scenario 5: Multiple Policies - Default Policy Behavior

    [Fact]
    [IntegrationTest]
    public async Task Scenario5_MultiplePolicies_FirstPolicyIsUsedAsDefault()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-multiple-origins.json");

      try
      {
        // Request with first policy's origin
        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://first.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderPresent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderValue(response, "Access-Control-Allow-Origin", "https://first.com");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task Scenario5_MultiplePolicies_SecondPolicyOrigin_NotAllowed()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-multiple-origins.json");

      try
      {
        // Request with second policy's origin - should NOT work because only first policy is applied
        var request = CreateSimpleRequest("/api/test", HttpMethod.Post, "https://second.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Server responds
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Origin"); // But no CORS headers
      }
      finally
      {
        await service.StopAsync();
      }
    }

    #endregion

    #region Scenario 6: No CORS Extension

    [Fact]
    [IntegrationTest]
    public async Task Scenario6_NoCorsExtension_NoCorsHeaders()
    {
      // Arrange
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      var config = new ConfigurationBuilder().Build();

      var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        // NOTE: No .WithCORS() call
        .ConfigureApiPipeline(endpoints =>
        {
          endpoints.MapGet("/api/test", () => Results.Ok(new { message = "Test endpoint" }));
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      try
      {
        var server = ((MicroService)microservice).Host.GetTestServer();
        var client = server.CreateClient();

        var request = CreateSimpleRequest("/api/test", HttpMethod.Get, "https://example.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Origin");
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Methods");
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Headers");
      }
      finally
      {
        await microservice.StopAsync();
      }
    }

    #endregion

    #region Scenario 7: Same-Origin Requests

    [Fact]
    [IntegrationTest]
    public async Task Scenario7_SameOriginRequest_NoOriginHeader_NoCorsHeaders()
    {
      // Arrange
      var (service, client) = await CreateTestServiceWithCORS("shared-logging-config.json", "cors-integration-allowany.json");

      try
      {
        // Request without Origin header (same-origin request)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
        // Note: No Origin header added

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // CORS headers should NOT be present for same-origin requests
        AssertCorsHeaderAbsent(response, "Access-Control-Allow-Origin");
      }
      finally
      {
        await service.StopAsync();
      }
    }

    #endregion
  }
}
