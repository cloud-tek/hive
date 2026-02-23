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

    var startTimestamp = Stopwatch.GetTimestamp();

    HttpResponseMessage? response = null;
    Exception? exception = null;
    try
    {
      response = await base.SendAsync(request, cancellationToken);
      return response;
    }
    catch (Exception ex)
    {
      exception = ex;
      throw;
    }
    finally
    {
      var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

      var statusCode = response?.StatusCode.ToString() ?? "error";
      var tags = new TagList
      {
        { "service.name", _serviceName },
        { "http.request.method", method },
        { "server.address", host },
        { "http.response.status_code", statusCode },
        { "client.name", _clientName }
      };

      if (exception is not null)
      {
        tags.Add("error.type", exception.GetType().Name);
      }

      HttpClientMeter.RequestDuration.Record(elapsed.TotalMilliseconds, tags);
      HttpClientMeter.RequestCount.Add(1, tags);

      if (response is null || !response.IsSuccessStatusCode)
      {
        HttpClientMeter.RequestErrors.Add(1, tags);
      }
    }
  }
}