using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudTek.Testing;
using FluentAssertions;
using Hive.Exceptions;
using Hive.MicroServices.Extensions;
using Hive.MicroServices.GraphQL;
using Hive.MicroServices.Grpc;
using Hive.MicroServices.Job;
using Hive.MicroServices.Mcp;
using Hive.MicroServices.Testing;
using Hive.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class MapEndpointsTests
  {
    private const string ServiceName = "microservice-tests-map-endpoints";

    // ─── Unit: recorder ──────────────────────────────────────────────────────

    [Fact]
    [UnitTest]
    public void GivenMapEndpoints_WhenCalledWithValidArgs_ThenActionIsAppended()
    {
      // Arrange
      var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>());

      // Act
      microservice.MapEndpoints(endpoints => endpoints.MapGet("/test", () => Results.Ok()));

      // Assert
      ((MicroService)microservice).MapEndpointActions.Should().HaveCount(1);
    }

    [Fact]
    [UnitTest]
    public void GivenMapEndpoints_WhenCalledMultipleTimes_ThenAllActionsAreAppended()
    {
      // Arrange
      var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>());

      // Act
      microservice
        .MapEndpoints(endpoints => endpoints.MapGet("/a", () => Results.Ok()))
        .MapEndpoints(endpoints => endpoints.MapGet("/b", () => Results.Ok()));

      // Assert
      ((MicroService)microservice).MapEndpointActions.Should().HaveCount(2);
    }

    [Fact]
    [UnitTest]
    public void GivenMapEndpoints_WhenMicroserviceIsNull_ThenThrowsArgumentNullException()
    {
      // Arrange
      IMicroService? microservice = null;

      // Act
      var act = () => microservice!.MapEndpoints(endpoints => { });

      // Assert
      act.Should().Throw<ArgumentNullException>().WithParameterName("microservice");
    }

    [Fact]
    [UnitTest]
    public void GivenMapEndpoints_WhenMapIsNull_ThenThrowsArgumentNullException()
    {
      // Arrange
      IMicroService microservice = new MicroService(ServiceName, new NullLogger<IMicroService>());

      // Act
      var act = () => microservice.MapEndpoints(null!);

      // Assert
      act.Should().Throw<ArgumentNullException>().WithParameterName("map");
    }

    [Fact]
    [UnitTest]
    public void GivenMapEndpoints_WhenCalled_ThenDoesNotInspectPipelineMode()
    {
      // Arrange — no pipeline configured, PipelineMode is NotSet
      var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>());

      // Act — must not throw regardless of PipelineMode
      var act = () => microservice.MapEndpoints(endpoints => { });

      // Assert
      act.Should().NotThrow();
      ((MicroService)microservice).PipelineMode.Should().Be(MicroServicePipelineMode.NotSet);
    }

    // ─── Integration: MCP + custom route share DI singleton ──────────────────

    [Fact]
    [IntegrationTest]
    public async Task GivenMcpPipelineWithMapEndpoints_WhenRequesting_ThenCustomRouteResponds()
    {
      // Arrange
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureServices((services, _) =>
        {
          services.AddSingleton<SharedCounter>();
        })
        .ConfigureMcpPipeline(mcp => { })
        .MapEndpoints(routes =>
        {
          routes.MapPost("/admin/flush", (SharedCounter counter) =>
          {
            counter.Increment();
            return Results.Ok(new { flushed = counter.Value });
          });
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      try
      {
        var server = ((MicroService)microservice).Host.GetTestServer();
        var client = server.CreateClient();

        // Act — custom route must respond
        var response = await client.PostAsync("/admin/flush", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("flushed");
      }
      finally
      {
        await microservice.StopAsync();
      }
    }

    [Fact]
    [IntegrationTest]
    public async Task GivenMcpPipelineWithMapEndpoints_WhenCustomRouteUsedSingleton_ThenSameInstanceAsFromDi()
    {
      // Arrange
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      SharedCounter? capturedFromEndpoint = null;

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureServices((services, _) =>
        {
          services.AddSingleton<SharedCounter>();
        })
        .ConfigureMcpPipeline(mcp => { })
        .MapEndpoints(routes =>
        {
          routes.MapGet("/admin/counter", (SharedCounter counter) =>
          {
            capturedFromEndpoint = counter;
            return Results.Ok(new { value = counter.Value });
          });
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      try
      {
        var server = ((MicroService)microservice).Host.GetTestServer();
        var client = server.CreateClient();

        // Act — resolve via HTTP (triggers DI)
        var response = await client.GetAsync("/admin/counter");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Also resolve directly from service provider — must be the same singleton
        var concrete = (MicroService)microservice;
        var fromDi = concrete.ServiceProvider.GetRequiredService<SharedCounter>();

        // Assert — same instance (singleton)
        capturedFromEndpoint.Should().NotBeNull();
        capturedFromEndpoint.Should().BeSameAs(fromDi);
      }
      finally
      {
        await microservice.StopAsync();
      }
    }

    // ─── Integration: GraphQL spot-check ─────────────────────────────────────

    [Fact]
    [IntegrationTest]
    public async Task GivenGraphQLPipelineWithMapEndpoints_WhenCustomRouteRequested_ThenRespondsOk()
    {
      // Arrange
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureGraphQLPipeline(schema =>
        {
          schema.AddDocumentFromString("type Query { ping: String }");
          schema.AddResolver("Query", "ping", _ => "pong");
        })
        .MapEndpoints(routes =>
        {
          routes.MapGet("/admin/ping", () => Results.Ok(new { ping = "pong" }));
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      try
      {
        var server = ((MicroService)microservice).Host.GetTestServer();
        var client = server.CreateClient();

        // Act — custom route must respond 200
        var pingResponse = await client.GetAsync("/admin/ping");

        // Assert
        pingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await pingResponse.Content.ReadAsStringAsync();
        content.Should().Contain("pong");
      }
      finally
      {
        await microservice.StopAsync();
      }
    }

    // ─── Integration: gRPC catch-all ordering ────────────────────────────────

    [Fact]
    [IntegrationTest]
    public async Task GivenGrpcPipelineWithMapEndpoints_WhenRequesting_ThenCustomRouteRespondsAndCatchAllStaysLast()
    {
      // Arrange
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureGrpcPipeline(endpoints => { })
        .MapEndpoints(routes =>
        {
          routes.MapGet("/healthz-custom", () => Results.Ok(new { status = "healthy" }));
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      try
      {
        var server = ((MicroService)microservice).Host.GetTestServer();
        var client = server.CreateClient();

        // Act — custom route must resolve
        var customResponse = await client.GetAsync("/healthz-custom");
        customResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — gRPC catch-all must still return its guidance string (remains last)
        var catchAllResponse = await client.GetAsync("/");
        catchAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var catchAllContent = await catchAllResponse.Content.ReadAsStringAsync();
        catchAllContent.Should().Contain("gRPC client");
      }
      finally
      {
        await microservice.StopAsync();
      }
    }

    // ─── Integration: Default-None drains custom routes ──────────────────────

    [Fact]
    [IntegrationTest]
    public async Task GivenDefaultServicePipelineWithMapEndpoints_WhenRequesting_ThenCustomRouteResponds()
    {
      // Arrange
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureDefaultServicePipeline()
        .MapEndpoints(routes =>
        {
          routes.MapGet("/admin/ping", () => Results.Ok(new { ping = "pong" }));
        })
        .ConfigureTestHost();

      await microservice.InitializeAsync(config);
      await microservice.StartAsync();

      try
      {
        var server = ((MicroService)microservice).Host.GetTestServer();
        var client = server.CreateClient();

        // Act — custom route must respond 200
        var customResponse = await client.GetAsync("/admin/ping");
        customResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — unmapped path must still return 404 via catch-all
        var notFoundResponse = await client.GetAsync("/does-not-exist");
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
      }
      finally
      {
        await microservice.StopAsync();
      }
    }

    // ─── Integration: Job rejection ──────────────────────────────────────────

    [Fact]
    [IntegrationTest]
    public async Task GivenJobPipeline_WhenMapEndpointsCalledAfterConfigureJob_ThenThrowsConfigurationException()
    {
      // Arrange — MapEndpoints AFTER ConfigureJob
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureJob()
        .MapEndpoints(routes => routes.MapGet("/admin/ping", () => Results.Ok()))
        .ConfigureTestHost();

      // Act
      var act = async () =>
      {
        await microservice.InitializeAsync(config);
        await microservice.StartAsync();
      };

      // Assert
      var ex = await act.Should().ThrowAsync<ConfigurationException>();
      ex.And.Message.Should().Be(Constants.Errors.MapEndpointsJobForbidden);
    }

    [Fact]
    [IntegrationTest]
    public async Task GivenJobPipeline_WhenMapEndpointsCalledBeforeConfigureJob_ThenThrowsConfigurationException()
    {
      // Arrange — MapEndpoints BEFORE ConfigureJob (proves guard is at build time)
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .MapEndpoints(routes => routes.MapGet("/admin/ping", () => Results.Ok()))
        .ConfigureJob()
        .ConfigureTestHost();

      // Act
      var act = async () =>
      {
        await microservice.InitializeAsync(config);
        await microservice.StartAsync();
      };

      // Assert — same exception regardless of call order
      var ex = await act.Should().ThrowAsync<ConfigurationException>();
      ex.And.Message.Should().Be(Constants.Errors.MapEndpointsJobForbidden);
    }

    [Fact]
    [IntegrationTest]
    public async Task GivenJobPipeline_WhenNoMapEndpointsCalled_ThenServiceStartsNormally()
    {
      // Arrange — Job with NO MapEndpoints (negative case)
      using var scope = EnvironmentVariableScope.Create(
        Constants.EnvironmentVariables.DotNet.Environment, "Development");

      var config = new ConfigurationBuilder().Build();

      await using var microservice = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureJob()
        .ConfigureTestHost();

      // Act
      await microservice.InitializeAsync(config);
      var act = async () => await microservice.StartAsync();

      // Assert — no exception; service starts
      await act.Should().NotThrowAsync();
      await microservice.StopAsync();
    }

    // ─── Helper types ─────────────────────────────────────────────────────────

    /// <summary>
    /// Shared singleton used to prove DI instance identity across MCP tools and custom endpoints.
    /// </summary>
    internal sealed class SharedCounter
    {
      private int _value;

      public int Value => _value;

      public void Increment() => System.Threading.Interlocked.Increment(ref _value);
    }
  }
}