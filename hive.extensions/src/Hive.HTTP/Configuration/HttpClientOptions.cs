namespace Hive.HTTP.Configuration;

public class HttpClientOptions
{
  public const string SectionKey = "Hive:Http";

  public string? BaseAddress { get; set; }

  public HttpClientFlavour Flavour { get; set; } = HttpClientFlavour.Internal;

  public ResilienceOptions Resilience { get; set; } = new();

  public AuthenticationOptions? Authentication { get; set; }

  public SocketsHandlerOptions SocketsHandler { get; set; } = new();
}