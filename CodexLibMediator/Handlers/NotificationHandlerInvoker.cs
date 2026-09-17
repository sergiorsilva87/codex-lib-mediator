using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Handlers;

internal sealed class NotificationHandlerInvoker<TNotification>(IServiceProvider serviceProvider) : INotificationHandlerInvoker
    where TNotification : INotification
{
    public Task InvokeAsync(object notification, CancellationToken cancellationToken)
    {
        var typedNotification = (TNotification)notification;
        var handlers = serviceProvider
            .GetServices<INotificationHandler<TNotification>>();

        return InvokeHandlersAsync(typedNotification, handlers, cancellationToken);
    }

    private static async Task InvokeHandlersAsync(TNotification notification,
        IEnumerable<INotificationHandler<TNotification>> handlers, CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
            await handler.HandleAsync(notification, cancellationToken);
    }
}