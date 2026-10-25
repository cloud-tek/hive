using System.Threading.Tasks;
using CloudTek.Testing;
using FluentAssertions;
using Hive.Exceptions;
using Hive.MicroServices.Extensions;
using Hive.MicroServices.Job;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.MicroServices.Tests;

public class CustomEndpointsGuardTests
{
  private const string ServiceName = "custom-endpoints-guard-tests";

  [Fact]
  [UnitTest]
  public void ConfigureJob_when_called_sets_pipeline_mode_to_job()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    service.ConfigureJob();

    // Assert
    service.PipelineMode.Should().Be(MicroServicePipelineMode.Job);
  }

  [Fact]
  [UnitTest]
  public void ConfigureDefaultServicePipeline_when_called_keeps_pipeline_mode_none()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    service.ConfigureDefaultServicePipeline();

    // Assert
    service.PipelineMode.Should().Be(MicroServicePipelineMode.None);
  }

  [Fact]
  [UnitTest]
  public void PipelineMode_job_value_is_appended_without_renumbering_existing_values()
  {
    // Arrange / Act / Assert
    ((int)MicroServicePipelineMode.NotSet).Should().Be(0);
    ((int)MicroServicePipelineMode.None).Should().Be(1);
    ((int)MicroServicePipelineMode.ApiControllers).Should().Be(2);
    ((int)MicroServicePipelineMode.Api).Should().Be(3);
    ((int)MicroServicePipelineMode.GraphQL).Should().Be(4);
    ((int)MicroServicePipelineMode.Grpc).Should().Be(5);
    ((int)MicroServicePipelineMode.Mcp).Should().Be(6);
    ((int)MicroServicePipelineMode.Job).Should().Be(7);
  }

  [Fact]
  [UnitTest]
  public void Guard_job_mode_with_custom_endpoints_throws_configuration_exception()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());
    service.PipelineMode = MicroServicePipelineMode.Job;
    service.MapEndpoints(e => e.MapGet("/x", () => Results.Ok()));

    // Act
    var act = () => service.ThrowIfCustomEndpointsForbidden();

    // Assert
    act.Should().Throw<ConfigurationException>().WithMessage(Constants.Errors.MapEndpointsJobForbidden);
  }

  [Fact]
  [UnitTest]
  public void Guard_job_mode_without_custom_endpoints_does_not_throw()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());
    service.PipelineMode = MicroServicePipelineMode.Job;

    // Act
    var act = () => service.ThrowIfCustomEndpointsForbidden();

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  [UnitTest]
  public void Guard_none_mode_with_custom_endpoints_does_not_throw()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());
    service.PipelineMode = MicroServicePipelineMode.None;
    service.MapEndpoints(e => e.MapGet("/x", () => Results.Ok()));

    // Act
    var act = () => service.ThrowIfCustomEndpointsForbidden();

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  [UnitTest]
  public async Task DrainCustomEndpoints_job_mode_throws_before_invoking_map_actions()
  {
    // Arrange
    var invoked = false;
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());
    service.PipelineMode = MicroServicePipelineMode.Job;
    service.MapEndpoints(_ => invoked = true);
    await using var app = WebApplication.CreateBuilder().Build();

    // Act
    var act = () => app.DrainCustomEndpoints(service);

    // Assert
    act.Should().Throw<ConfigurationException>();
    invoked.Should().BeFalse();
  }
}