namespace Hive.Messaging.Handling;

public abstract class HiveMessageHandler<TMessage>
{
  public abstract Task HandleAsync(TMessage message, CancellationToken ct);

  protected virtual Task OnRetryAsync(TMessage message, Exception exception, int attempt, CancellationToken ct)
    => Task.CompletedTask;

  protected virtual Task OnErrorAsync(TMessage message, Exception exception, CancellationToken ct)
    => Task.CompletedTask;

  protected virtual Task OnSuccessAsync(TMessage message, CancellationToken ct)
    => Task.CompletedTask;
}

public abstract class HiveMessageHandler<TMessage, TResponse>
{
  public abstract Task<TResponse> HandleAsync(TMessage message, CancellationToken ct);

  protected virtual Task OnRetryAsync(TMessage message, Exception exception, int attempt, CancellationToken ct)
    => Task.CompletedTask;

  protected virtual Task OnErrorAsync(TMessage message, Exception exception, CancellationToken ct)
    => Task.CompletedTask;

  protected virtual Task OnSuccessAsync(TMessage message, TResponse response, CancellationToken ct)
    => Task.CompletedTask;
}
