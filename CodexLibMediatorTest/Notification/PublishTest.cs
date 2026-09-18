using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Notification;

public class PublishTest
{
    private static readonly List<string> ExecutionOrder = [];

    public static bool Executed { get; private set; }

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

        Assert.Equal([nameof(SecondTestNotificationHandler), nameof(TestNotificationHandler)], ExecutionOrder);
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
        services.AddCodexMediator(typeof(IMediator).Assembly);
        
        services.AddTransient<INotificationHandler<OrderedFailureNotification>, FirstOrderedFailureHandler>();
        services.AddTransient<INotificationHandler<OrderedFailureNotification>, FailingOrderedFailureHandler>();
        services.AddTransient<INotificationHandler<OrderedFailureNotification>, LastOrderedFailureHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        FirstOrderedFailureHandler.Reset();
        LastOrderedFailureHandler.Reset();

        var notification = new OrderedFailureNotification();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            mediator.PublishAsync(notification));

        Assert.Equal("Notification handler failed.", exception.Message);
        Assert.True(FirstOrderedFailureHandler.WasExecuted);
        Assert.False(LastOrderedFailureHandler.WasExecuted);
    }
    
    [Fact]
    public async Task PublishAsync_ShouldExecuteNotificationHandlersInDeterministicOrder()
    {
        var services = new ServiceCollection();

        services.AddCodexMediator(typeof(OrderedNotificationHandlerA).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        OrderedNotificationHandlerA.Reset();
        OrderedNotificationHandlerB.Reset();

        await mediator.PublishAsync(new OrderedNotification());

        Assert.Equal(
            [
                nameof(OrderedNotificationHandlerA),
                nameof(OrderedNotificationHandlerB)
            ],
            ExecutionOrder);
    }
    
    [Fact]
    public async Task PublishAsync_Should_Complete_When_Assembly_Has_No_Notification_Handler()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(IMediator).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new UnhandledNotification());
    }
    
    [Fact]
    public async Task PublishAsync_ShouldExecuteNotificationHandlerTwice_WhenRegisteredTwice()
    {
        var services = new ServiceCollection();

        services.AddCodexMediator(typeof(IMediator).Assembly);
        services.AddTransient<INotificationHandler<DuplicatedNotification>, DuplicatedNotificationHandler>();
        services.AddTransient<INotificationHandler<DuplicatedNotification>, DuplicatedNotificationHandler>();

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        DuplicatedNotificationHandler.Reset();
        await mediator.PublishAsync(new DuplicatedNotification());

        Assert.Equal(2, DuplicatedNotificationHandler.ExecutionCount);
    }
    
    [Fact]
    public async Task PublishAsync_ShouldExecuteNotificationHandlerTwice_WhenMediatorIsRegisteredTwice()
    {
        var services = new ServiceCollection();

        var assembly = typeof(DuplicatedRegistrationNotificationHandler).Assembly;

        services.AddCodexMediator(assembly);
        services.AddCodexMediator(assembly);

        await using var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        DuplicatedRegistrationNotificationHandler.Reset();

        await mediator.PublishAsync(new DuplicatedRegistrationNotification());

        Assert.Equal(2, DuplicatedRegistrationNotificationHandler.ExecutionCount);
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

        public Task HandleAsync(OrderedFailureNotification notification, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }

        public static void Reset()
        {
            WasExecuted = false;
        }
    }

    private sealed class FailingOrderedFailureHandler : INotificationHandler<OrderedFailureNotification>
    {
        public Task HandleAsync(OrderedFailureNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Notification handler failed.");
        }
    }

    private sealed class LastOrderedFailureHandler : INotificationHandler<OrderedFailureNotification>
    {
        public static bool WasExecuted { get; private set; }

        public Task HandleAsync(OrderedFailureNotification notification, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }

        public static void Reset()
        {
            WasExecuted = false;
        }
    }
    
    private sealed class OrderedNotification : INotification
    {
    }
    
    private sealed class OrderedNotificationHandlerA : INotificationHandler<OrderedNotification>
    {
        public Task HandleAsync(OrderedNotification notification, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add(nameof(OrderedNotificationHandlerA));
            return Task.CompletedTask;
        }

        public static void Reset()
        {
            ExecutionOrder.Clear();
        }
    }
    
    private sealed class OrderedNotificationHandlerB : INotificationHandler<OrderedNotification>
    {
        public Task HandleAsync(OrderedNotification notification, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add(nameof(OrderedNotificationHandlerB));
            return Task.CompletedTask;
        }

        public static void Reset()
        {
        }
    }
    
    private sealed class DuplicatedNotification : INotification
    {
    }

    private sealed class DuplicatedNotificationHandler : INotificationHandler<DuplicatedNotification>
    {
        public static int ExecutionCount { get; private set; }

        public Task HandleAsync(DuplicatedNotification notification, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }

        public static void Reset()
        {
            ExecutionCount = 0;
        }
    }
    
    private sealed class DuplicatedRegistrationNotification : INotification
    {
    }

    private sealed class DuplicatedRegistrationNotificationHandler : INotificationHandler<DuplicatedRegistrationNotification>
    {
        private static int _executionCount;

        public static int ExecutionCount => _executionCount;

        public static void Reset()
        {
            _executionCount = 0;
        }

        public Task HandleAsync(DuplicatedRegistrationNotification notification, CancellationToken cancellationToken)
        {
            _executionCount++;
            return Task.CompletedTask;
        }
    }
}