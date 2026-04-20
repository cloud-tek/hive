using CloudTek.Testing;
using FluentAssertions;
using Hive.Messaging.Handling;
using Hive.Messaging.Tests.TestFixtures;
using Xunit;

namespace Hive.Messaging.Tests;

public class HandlerLifecycleTests
{
  [Fact]
  [UnitTest]
  public async Task GivenFireAndForgetHandler_WhenHandleAsyncCalled_ThenMessageIsProcessed()
  {
    var handler = new TestMessageHandler();
    var message = new TestMessage("test");

    await handler.HandleAsync(message, CancellationToken.None);

    handler.HandleCount.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public async Task GivenCascadingHandler_WhenHandleAsyncCalled_ThenResponseIsReturned()
  {
    var handler = new TestRequestHandler();
    var request = new TestRequest("hello");

    var response = await handler.HandleAsync(request, CancellationToken.None);

    response.Should().NotBeNull();
    response.Result.Should().Be("Response to: hello");
  }

  [Fact]
  [UnitTest]
  public async Task GivenFailingHandler_WhenHandleAsyncCalled_ThenExceptionIsThrown()
  {
    var handler = new FailingMessageHandler();
    var message = new TestMessage("fail");

    var act = () => handler.HandleAsync(message, CancellationToken.None);

    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("Handler failed");
  }

  [Fact]
  [UnitTest]
  public async Task GivenHandler_WhenOnSuccessAsyncCalled_ThenDefaultReturnsCompletedTask()
  {
    var handler = new MinimalHandler();
    var message = new TestMessage("test");

    await handler.HandleAsync(message, CancellationToken.None);

    handler.Handled.Should().BeTrue();
  }

  private sealed class MinimalHandler : HiveMessageHandler<TestMessage>
  {
    public bool Handled { get; private set; }

    public override Task HandleAsync(TestMessage message, CancellationToken ct)
    {
      Handled = true;
      return Task.CompletedTask;
    }
  }
}