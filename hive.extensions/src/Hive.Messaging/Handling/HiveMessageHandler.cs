namespace Hive.Messaging.Handling;

/// <summary>
/// Base class for message handlers that process a message without returning a response.
/// </summary>
/// <typeparam name="TMessage">The type of message to handle.</typeparam>
public abstract class HiveMessageHandler<TMessage>
{
  /// <summary>
  /// Handles the incoming message.
  /// </summary>
  /// <param name="message">The message to handle.</param>
  /// <param name="ct">Cancellation token.</param>
  public abstract Task HandleAsync(TMessage message, CancellationToken ct);

  /// <summary>
  /// Called when a retry attempt is made after a transient failure.
  /// </summary>
  /// <param name="message">The message being retried.</param>
  /// <param name="exception">The exception that triggered the retry.</param>
  /// <param name="attempt">The retry attempt number.</param>
  /// <param name="ct">Cancellation token.</param>
  protected virtual Task OnRetryAsync(TMessage message, Exception exception, int attempt, CancellationToken ct)
    => Task.CompletedTask;

  /// <summary>
  /// Called when message handling fails after all retries are exhausted.
  /// </summary>
  /// <param name="message">The message that failed.</param>
  /// <param name="exception">The final exception.</param>
  /// <param name="ct">Cancellation token.</param>
  protected virtual Task OnErrorAsync(TMessage message, Exception exception, CancellationToken ct)
    => Task.CompletedTask;

  /// <summary>
  /// Called after successful message handling.
  /// </summary>
  /// <param name="message">The message that was handled.</param>
  /// <param name="ct">Cancellation token.</param>
  protected virtual Task OnSuccessAsync(TMessage message, CancellationToken ct)
    => Task.CompletedTask;
}

/// <summary>
/// Base class for message handlers that process a message and return a response.
/// </summary>
/// <typeparam name="TMessage">The type of message to handle.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class HiveMessageHandler<TMessage, TResponse>
{
  /// <summary>
  /// Handles the incoming message and returns a response.
  /// </summary>
  /// <param name="message">The message to handle.</param>
  /// <param name="ct">Cancellation token.</param>
  public abstract Task<TResponse> HandleAsync(TMessage message, CancellationToken ct);

  /// <summary>
  /// Called when a retry attempt is made after a transient failure.
  /// </summary>
  /// <param name="message">The message being retried.</param>
  /// <param name="exception">The exception that triggered the retry.</param>
  /// <param name="attempt">The retry attempt number.</param>
  /// <param name="ct">Cancellation token.</param>
  protected virtual Task OnRetryAsync(TMessage message, Exception exception, int attempt, CancellationToken ct)
    => Task.CompletedTask;

  /// <summary>
  /// Called when message handling fails after all retries are exhausted.
  /// </summary>
  /// <param name="message">The message that failed.</param>
  /// <param name="exception">The final exception.</param>
  /// <param name="ct">Cancellation token.</param>
  protected virtual Task OnErrorAsync(TMessage message, Exception exception, CancellationToken ct)
    => Task.CompletedTask;

  /// <summary>
  /// Called after successful message handling.
  /// </summary>
  /// <param name="message">The message that was handled.</param>
  /// <param name="response">The response produced by the handler.</param>
  /// <param name="ct">Cancellation token.</param>
  protected virtual Task OnSuccessAsync(TMessage message, TResponse response, CancellationToken ct)
    => Task.CompletedTask;
}
