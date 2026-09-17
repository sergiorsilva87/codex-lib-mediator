using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Notification;

public class PublishTest
{
    private static readonly List<string> ExecutionOrder = [];

    [Fact]
    public async Task PublishAsync_Should_Invoke_Notification_Handler()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestNotificationHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var notification = new TestNotification { Message = "Hello Notification" };
        await mediator.PublishAsync(notification);

        Assert.Equal("Hello Notification", TestNotificationHandler.ReceivedMessage);
    }

    [Fact]
    public async Task PublishAsync_Should_Invoke_All_Notification_Handlers()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestNotificationHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var notification = new TestNotification { Message = "Hello Notification" };

        ExecutionOrder.Clear();
        await mediator.PublishAsync(notification);

        Assert.Equal([nameof(TestNotificationHandler), nameof(SecondTestNotificationHandler)], ExecutionOrder);
        Assert.Equal("Hello Notification", TestNotificationHandler.ReceivedMessage);
        Assert.Equal("Hello Notification", SecondTestNotificationHandler.ReceivedMessage);
    }

    [Fact]
    public async Task PublishAsync_Should_Pass_CancellationToken_To_Notification_Handler()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestNotificationHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var notification = new TestNotification { Message = "Hello Notification" };

        await mediator.PublishAsync(notification, cancellationToken);
        Assert.Equal(cancellationToken, TestNotificationHandler.ReceivedCancellationToken);
    }

    [Fact]
    public async Task PublishAsync_Should_Complete_When_No_Handler_Is_Registered()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestNotificationHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var notification = new UnhandledNotification();

        await mediator.PublishAsync(notification);
    }

    [Fact]
    public async Task PublishAsync_Should_Propagate_Notification_Handler_Exception()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestNotificationHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var notification = new FailingNotification();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.PublishAsync(notification));
        Assert.Equal("Notification handler failed.", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_Should_Propagate_Cancellation_When_Handler_Throws()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestNotificationHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var notification = new CancellationNotification();

        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            mediator.PublishAsync(notification, cancellationTokenSource.Token));
    }
    
    [Fact]
    public async Task PublishAsync_Should_Stop_Execution_When_Notification_Handler_Fails()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(FirstOrderedFailureHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        FirstOrderedFailureHandler.Reset();
        LastOrderedFailureHandler.Reset();

        var notification = new OrderedFailureNotification();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(notification));

        Assert.Equal("Notification handler failed.", exception.Message);
        Assert.True(FirstOrderedFailureHandler.WasExecuted);
        Assert.False(LastOrderedFailureHandler.WasExecuted);
    }

    private sealed class TestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public static string? ReceivedMessage { get; private set; }
        public static CancellationToken ReceivedCancellationToken { get; private set; }

        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            ReceivedMessage = notification.Message;
            ReceivedCancellationToken = cancellationToken;

            ExecutionOrder.Add(nameof(TestNotificationHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class SecondTestNotificationHandler : INotificationHandler<TestNotification>
    {
        public static string? ReceivedMessage { get; private set; }

        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            ReceivedMessage = notification.Message;
            ExecutionOrder.Add(nameof(SecondTestNotificationHandler));

            return Task.CompletedTask;
        }
    }

    private sealed class UnhandledNotification : INotification
    {
    }

    private sealed class FailingNotificationHandler : INotificationHandler<FailingNotification>
    {
        public Task HandleAsync(FailingNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Notification handler failed.");
        }
    }

    private sealed class FailingNotification : INotification
    {
    }

    private sealed class CancellationNotification : INotification
    {
    }

    private sealed class CancellationNotificationHandler : INotificationHandler<CancellationNotification>
    {
        public static bool Executed { get; private set; }

        public Task HandleAsync(CancellationNotification notification, CancellationToken cancellationToken)
        {
            Executed = true;
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }
    
    private sealed class OrderedFailureNotification : INotification
    {
    }
    
    private sealed class FirstOrderedFailureHandler : INotificationHandler<OrderedFailureNotification>
    {
        public static bool WasExecuted { get; private set; }

        public static void Reset()
        {
            WasExecuted = false;
        }

        public Task HandleAsync(OrderedFailureNotification notification, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }
    
    private sealed class FailingOrderedFailureHandler : INotificationHandler<OrderedFailureNotification>
    {
        public Task HandleAsync(OrderedFailureNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Notification handler failed.");
        }
    }
    
    public static bool Executed { get; private set; }
    
    private sealed class LastOrderedFailureHandler : INotificationHandler<OrderedFailureNotification>
    {
        public static bool WasExecuted { get; private set; }

        public static void Reset()
        {
            WasExecuted = false;
        }

        public Task HandleAsync(OrderedFailureNotification notification, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }
}