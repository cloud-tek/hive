using Hive.Messaging.Handling;

namespace Hive.Messaging.Tests.TestFixtures;

public class TestMessageHandler : HiveMessageHandler<TestMessage>
{
  public int HandleCount { get; private set; }
  public int SuccessCount { get; private set; }
  public int ErrorCount { get; private set; }
  public int RetryCount { get; private set; }

  public override Task HandleAsync(TestMessage message, CancellationToken ct)
  {
    HandleCount++;
    return Task.CompletedTask;
  }

  protected override Task OnSuccessAsync(TestMessage message, CancellationToken ct)
  {
    SuccessCount++;
    return Task.CompletedTask;
  }

  protected override Task OnErrorAsync(TestMessage message, Exception exception, CancellationToken ct)
  {
    ErrorCount++;
    return Task.CompletedTask;
  }

  protected override Task OnRetryAsync(TestMessage message, Exception exception, int attempt, CancellationToken ct)
  {
    RetryCount++;
    return Task.CompletedTask;
  }
}

public class TestRequestHandler : HiveMessageHandler<TestRequest, TestResponse>
{
  public override Task<TestResponse> HandleAsync(TestRequest message, CancellationToken ct)
  {
    return Task.FromResult(new TestResponse($"Response to: {message.Query}"));
  }
}

public class FailingMessageHandler : HiveMessageHandler<TestMessage>
{
  public override Task HandleAsync(TestMessage message, CancellationToken ct)
  {
    throw new InvalidOperationException("Handler failed");
  }
}
