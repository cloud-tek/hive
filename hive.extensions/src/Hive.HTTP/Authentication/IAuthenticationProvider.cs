namespace Hive.HTTP.Authentication;

/// <summary>
/// Defines a provider that applies authentication credentials to outgoing HTTP requests.
/// </summary>
public interface IAuthenticationProvider
{
  /// <summary>
  /// Applies authentication credentials to the specified HTTP request message.
  /// </summary>
  /// <param name="message">The outgoing HTTP request to authenticate.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken);
}
