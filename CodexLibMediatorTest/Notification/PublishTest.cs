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
}