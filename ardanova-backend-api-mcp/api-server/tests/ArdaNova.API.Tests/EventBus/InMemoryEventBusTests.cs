using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArdaNova.API.Tests.EventBus;

public class InMemoryEventBusTests
{
    private readonly InMemoryEventBus _eventBus;
    private readonly ServiceProvider _serviceProvider;

    public InMemoryEventBusTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();

        _eventBus = new InMemoryEventBus(
            _serviceProvider,
            NullLogger<InMemoryEventBus>.Instance);
    }

    [Fact]
    public async Task PublishAsync_WithAsyncSubscription_InvokesHandler()
    {
        // Arrange
        var receivedEvent = (TestEvent?)null;
        var handlerCalled = new TaskCompletionSource<bool>();

        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            receivedEvent = evt;
            handlerCalled.SetResult(true);
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent("Test message");

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        var result = await Task.WhenAny(handlerCalled.Task, Task.Delay(1000));
        result.Should().Be(handlerCalled.Task, "handler should have been called within timeout");

        receivedEvent.Should().NotBeNull();
        receivedEvent!.Message.Should().Be("Test message");
        receivedEvent.EventType.Should().Be("test.event");
    }

    [Fact]
    public async Task PublishAsync_WithSyncSubscription_InvokesHandler()
    {
        // Arrange
        var receivedEvent = (TestEvent?)null;
        var handlerCalled = new TaskCompletionSource<bool>();

        _eventBus.Subscribe<TestEvent>((evt) =>
        {
            receivedEvent = evt;
            handlerCalled.SetResult(true);
        });

        var testEvent = new TestEvent("Sync test");

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        var result = await Task.WhenAny(handlerCalled.Task, Task.Delay(1000));
        result.Should().Be(handlerCalled.Task, "handler should have been called within timeout");

        receivedEvent.Should().NotBeNull();
        receivedEvent!.Message.Should().Be("Sync test");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleSubscriptions_InvokesAllHandlers()
    {
        // Arrange
        var callCount = 0;
        var allHandlersCalled = new TaskCompletionSource<bool>();

        _eventBus.Subscribe<TestEvent>(_ => Interlocked.Increment(ref callCount));
        _eventBus.Subscribe<TestEvent>(_ => Interlocked.Increment(ref callCount));
        _eventBus.Subscribe<TestEvent>(_ =>
        {
            Interlocked.Increment(ref callCount);
            if (callCount >= 3) allHandlersCalled.TrySetResult(true);
        });

        var testEvent = new TestEvent("Multi handler test");

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        var result = await Task.WhenAny(allHandlersCalled.Task, Task.Delay(1000));
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task Subscribe_ReturnsDisposable_ThatUnsubscribes()
    {
        // Arrange
        var callCount = 0;
        var subscription = _eventBus.Subscribe<TestEvent>(_ => Interlocked.Increment(ref callCount));

        // Act - first publish (subscribed)
        await _eventBus.PublishAsync(new TestEvent("First"));
        await Task.Delay(100); // Allow async processing

        var countAfterFirst = callCount;

        // Unsubscribe
        subscription.Dispose();

        // Act - second publish (unsubscribed)
        await _eventBus.PublishAsync(new TestEvent("Second"));
        await Task.Delay(100);

        // Assert
        countAfterFirst.Should().Be(1);
        callCount.Should().Be(1, "handler should not be called after unsubscribe");
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent("No subscribers");

        // Act
        var act = async () => await _eventBus.PublishAsync(testEvent);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithFailingHandler_DoesNotAffectOtherHandlers()
    {
        // Arrange
        var successfulHandlerCalled = false;
        var handlerCompleted = new TaskCompletionSource<bool>();

        _eventBus.Subscribe<TestEvent>(_ => throw new Exception("Handler error"));
        _eventBus.Subscribe<TestEvent>(_ =>
        {
            successfulHandlerCalled = true;
            handlerCompleted.SetResult(true);
        });

        var testEvent = new TestEvent("Error handling test");

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        var result = await Task.WhenAny(handlerCompleted.Task, Task.Delay(1000));
        successfulHandlerCalled.Should().BeTrue("second handler should still be called");
    }

    [Fact]
    public void EventId_IsUniqueForEachEvent()
    {
        // Arrange & Act
        var event1 = new TestEvent("Event 1");
        var event2 = new TestEvent("Event 2");

        // Assert
        event1.EventId.Should().NotBeNullOrEmpty();
        event2.EventId.Should().NotBeNullOrEmpty();
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void OccurredAt_IsSetToCurrentTime()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var testEvent = new TestEvent("Timestamp test");

        // Assert
        var after = DateTime.UtcNow.AddSeconds(1);
        testEvent.OccurredAt.Should().BeAfter(before);
        testEvent.OccurredAt.Should().BeBefore(after);
    }

    [Fact]
    public async Task PublishAsync_WithDIRegisteredHandler_InvokesHandler()
    {
        // Arrange - use a real handler class instead of mocking (due to Moq limitations with nested types)
        var handler = new TrackingEventHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        var sp = services.BuildServiceProvider();

        var eventBus = new InMemoryEventBus(sp, NullLogger<InMemoryEventBus>.Instance);
        var testEvent = new TestEvent("DI handler test");

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        handler.ReceivedEvents.Should().ContainSingle();
        handler.ReceivedEvents[0].Message.Should().Be("DI handler test");
    }

    private class TrackingEventHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = new();

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }
}

// Public record for Moq compatibility
public record TestEvent(string Message) : DomainEvent
{
    public override string EventType => "test.event";
}
