namespace Hive.HTTP.Authentication;

public interface IAuthenticationProvider
{
  Task ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken);
}