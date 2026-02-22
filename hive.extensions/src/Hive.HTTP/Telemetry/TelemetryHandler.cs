using System.Diagnostics;

namespace Hive.HTTP.Telemetry;

internal sealed class TelemetryHandler : DelegatingHandler
{
  private readonly string _serviceName;
  private readonly string _clientName;

  public TelemetryHandler(string serviceName, string clientName)
  {
    _serviceName = serviceName;
    _clientName = clientName;
  }

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    var method = request.Method.Method;
    var host = request.RequestUri?.Host ?? "unknown";

    var stopwatch = Stopwatch.StartNew();

    HttpResponseMessage? response = null;
    try
    {
      response = await base.SendAsync(request, cancellationToken);
      return response;
    }
    finally
    {
      stopwatch.Stop();

      var statusCode = response?.StatusCode.ToString() ?? "error";
      var tags = new TagList
      {
        { "service.name", _serviceName },
        { "http.request.method", method },
        { "server.address", host },
        { "http.response.status_code", statusCode },
        { "client.name", _clientName }
      };

      HttpClientMeter.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
      HttpClientMeter.RequestCount.Add(1, tags);

      if (response is null || !response.IsSuccessStatusCode)
      {
        HttpClientMeter.RequestErrors.Add(1, tags);
      }
    }
  }
}