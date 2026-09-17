namespace CodexLibMediator.Interfaces;

internal interface INotificationHandlerInvoker
{
    Task InvokeAsync(object notification, CancellationToken cancellationToken);
}