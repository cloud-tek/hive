using FluentAssertions;
using Hive.HTTP.Testing;
using Hive.MicroServices;
using Hive.MicroServices.Testing;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.HTTP.Tests;

public class ConfigurationTests
{
  private const string ServiceName = "http-config-tests";

  [Fact]
  [UnitTest]
  public async Task GivenConfigOnly_WhenBaseAddressProvided_ThenClientIsRegistered()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
        ["Hive:Http:IProductApi:Flavour"] = "Internal"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = JsonContent.Create(Array.Empty<string>())
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var provider = ((MicroService)service).Host!.Services;
    var api = provider.GetRequiredService<IProductApi>();
    api.Should().NotBeNull();

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfigOnly_WhenBaseAddressMissing_ThenStartupFails()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:Flavour"] = "Internal"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldFailToStart(TimeSpan.FromSeconds(5));
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfigOnly_WhenSectionMissing_ThenStartupFails()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldFailToStart(TimeSpan.FromSeconds(5));
  }

  [Fact]
  [UnitTest]
  public async Task GivenFluentOnly_WhenBaseAddressProvided_ThenClientIsRegistered()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("https://product-service"))
      .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = JsonContent.Create(Array.Empty<string>())
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var provider = ((MicroService)service).Host!.Services;
    var api = provider.GetRequiredService<IProductApi>();
    api.Should().NotBeNull();

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenFluentOnly_WhenBaseAddressMissing_ThenStartupFails()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>(client => client.Internal())
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldFailToStart(TimeSpan.FromSeconds(5));
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfigAndFluent_WhenFluentOverridesBaseAddress_ThenFluentWins()
  {
    HttpRequestMessage? capturedRequest = null;

    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://from-config",
        ["Hive:Http:IProductApi:Flavour"] = "Internal"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>(client => client
        .WithBaseAddress("https://from-fluent"))
      .WithMockResponse<IProductApi>(req =>
      {
        capturedRequest = req;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = JsonContent.Create(Array.Empty<string>())
        };
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var api = ((MicroService)service).Host!.Services.GetRequiredService<IProductApi>();
    await api.GetProducts();

    capturedRequest!.RequestUri!.Host.Should().Be("from-fluent");

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfigAndFluent_WhenConfigProvidesBaseAddressAndFluentAddsAuth_ThenBothApplied()
  {
    HttpRequestMessage? capturedRequest = null;

    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
        ["Hive:Http:IProductApi:Flavour"] = "Internal",
        ["Hive:Http:IProductApi:Resilience:MaxRetries"] = "3"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>(client => client
        .WithAuthentication(auth => auth.BearerToken(
          _ => _ => Task.FromResult("test-token"))))
      .WithMockResponse<IProductApi>(req =>
      {
        capturedRequest = req;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = JsonContent.Create(Array.Empty<string>())
        };
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var api = ((MicroService)service).Host!.Services.GetRequiredService<IProductApi>();
    await api.GetProducts();

    capturedRequest!.RequestUri!.Host.Should().Be("product-service");
    capturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
    capturedRequest!.Headers.Authorization!.Parameter.Should().Be("test-token");

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenMultipleClients_WhenMixingConfigAndFluent_ThenEachClientConfiguredIndependently()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
        ["Hive:Http:IProductApi:Flavour"] = "Internal"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .WithHttpClient<IInventoryApi>(client => client
        .Internal()
        .WithBaseAddress("https://inventory-service"))
      .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = JsonContent.Create(Array.Empty<string>())
      })
      .WithMockResponse<IInventoryApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = JsonContent.Create(Array.Empty<string>())
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var provider = ((MicroService)service).Host!.Services;
    provider.GetRequiredService<IProductApi>().Should().NotBeNull();
    provider.GetRequiredService<IInventoryApi>().Should().NotBeNull();

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenClientNameOverride_WhenConfigUsesCustomKey_ThenClientIsRegistered()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:ProductService:BaseAddress"] = "https://product-service",
        ["Hive:Http:ProductService:Flavour"] = "Internal"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>("ProductService")
      .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = JsonContent.Create(Array.Empty<string>())
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var provider = ((MicroService)service).Host!.Services;
    var api = provider.GetRequiredService<IProductApi>();
    api.Should().NotBeNull();

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfig_WhenBearerTokenTypeWithoutFluentAuth_ThenStartupFails()
  {
    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
        ["Hive:Http:IProductApi:Authentication:Type"] = "BearerToken"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldFailToStart(TimeSpan.FromSeconds(5));
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfig_WhenApiKeyAuthConfigured_ThenHeaderIsApplied()
  {
    HttpRequestMessage? capturedRequest = null;

    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
        ["Hive:Http:IProductApi:Authentication:Type"] = "ApiKey",
        ["Hive:Http:IProductApi:Authentication:HeaderName"] = "X-Api-Key",
        ["Hive:Http:IProductApi:Authentication:Value"] = "test-key-456"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .WithMockResponse<IProductApi>(req =>
      {
        capturedRequest = req;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = JsonContent.Create(Array.Empty<string>())
        };
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(5000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var api = ((MicroService)service).Host!.Services.GetRequiredService<IProductApi>();
    await api.GetProducts();

    capturedRequest!.Headers.GetValues("X-Api-Key").Should().ContainSingle("test-key-456");

    service.CancellationTokenSource.Cancel();
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfig_WhenResilienceConfigured_ThenResiliencePipelineIsActive()
  {
    var callCount = 0;

    var config = new ConfigurationBuilder()
      .UseDefaultLoggingConfiguration()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
        ["Hive:Http:IProductApi:Resilience:MaxRetries"] = "2"
      })
      .Build();

    var service = new MicroService(ServiceName)
      .InTestClass<ConfigurationTests>()
      .WithHttpClient<IProductApi>()
      .WithMockResponse<IProductApi>(_ =>
      {
        callCount++;
        return callCount <= 2
          ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
          : new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = JsonContent.Create(new[] { "Widget" })
          };
      })
      .ConfigureApiPipeline(app => { })
      .ConfigureTestHost();

    service.CancellationTokenSource.CancelAfter(15000);
    _ = service.RunAsync(config);

    service.ShouldStart(TimeSpan.FromSeconds(5));

    var api = ((MicroService)service).Host!.Services.GetRequiredService<IProductApi>();
    var products = await api.GetProducts();

    products.Should().Contain("Widget");
    callCount.Should().Be(3);

    service.CancellationTokenSource.Cancel();
  }
}