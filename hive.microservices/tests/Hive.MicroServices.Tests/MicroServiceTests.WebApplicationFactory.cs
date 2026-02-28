using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudTek.Testing;
using FluentAssertions;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using Hive.MicroServices.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class WebHostIntegration
  {
    private const string ServiceName = "microservice-webhost-tests";

    [Fact]
    [IntegrationTest]
    public async Task GivenMicroServiceWithConfigureWebHost_WhenCreatingTestServer_ThenServerUsesHiveConfiguration()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .ConfigureApiPipeline(endpoints =>
        {
          endpoints.MapGet("/api/test", () => Results.Ok(new { message = "Hello from Hive" }));
        })
        .ConfigureTestHost();

      // Act - Initialize and start using IMicroService APIs
      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      var server = ((MicroService)microservice).Host.GetTestServer();
      var client = server.CreateClient();
      var response = await client.GetAsync("/api/test");

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      var content = await response.Content.ReadAsStringAsync();
      content.Should().Contain("Hello from Hive");

      // Cleanup
      await microservice.StopAsync();
    }

    [Fact]
    [IntegrationTest]
    public async Task GivenMicroServiceWithMultipleEndpoints_WhenMakingRequests_ThenAllEndpointsRespond()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .ConfigureApiPipeline(endpoints =>
        {
          endpoints.MapGet("/api/users", () => Results.Ok(new[] { "Alice", "Bob" }));
          endpoints.MapGet("/api/products", () => Results.Ok(new[] { "Product1", "Product2" }));
          endpoints.MapPost("/api/orders", () => Results.Created("/api/orders/1", new { id = 1, status = "created" }));
        })
        .ConfigureTestHost();

      // Act - Initialize and start using IMicroService APIs
      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      var server = ((MicroService)microservice).Host.GetTestServer();
      var client = server.CreateClient();

      // Act & Assert - GET /api/users
      var usersResponse = await client.GetAsync("/api/users");
      usersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
      var usersContent = await usersResponse.Content.ReadAsStringAsync();
      usersContent.Should().Contain("Alice");

      // Act & Assert - GET /api/products
      var productsResponse = await client.GetAsync("/api/products");
      productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
      var productsContent = await productsResponse.Content.ReadAsStringAsync();
      productsContent.Should().Contain("Product1");

      // Act & Assert - POST /api/orders
      var ordersResponse = await client.PostAsync("/api/orders", null);
      ordersResponse.StatusCode.Should().Be(HttpStatusCode.Created);
      var ordersContent = await ordersResponse.Content.ReadAsStringAsync();
      ordersContent.Should().Contain("created");

      // Cleanup
      await microservice.StopAsync();
    }

    [Fact]
    [IntegrationTest]
    public async Task GivenMicroServiceWithCustomConfiguration_WhenAccessingEndpoints_ThenConfigurationIsApplied()
    {
      // Arrange
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["AppName"] = "TestApp"
        })
        .Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .ConfigureServices((services, cfg) =>
        {
          // Configuration is available here via cfg parameter
        })
        .ConfigureApiPipeline(endpoints =>
        {
          endpoints.MapGet("/api/config", (IConfiguration configuration) =>
            Results.Ok(new { appName = configuration["AppName"] }));
        })
        .ConfigureTestHost();

      // Act - Initialize and start using IMicroService APIs
      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      var server = ((MicroService)microservice).Host.GetTestServer();
      var client = server.CreateClient();
      var response = await client.GetAsync("/api/config");

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      var content = await response.Content.ReadAsStringAsync();
      content.Should().Contain("TestApp");

      // Cleanup
      await microservice.StopAsync();
    }
  }
}