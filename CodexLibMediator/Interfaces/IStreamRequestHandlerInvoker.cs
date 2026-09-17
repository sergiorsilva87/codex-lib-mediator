namespace CodexLibMediator.Interfaces;

internal interface IStreamRequestHandlerInvoker
{
    object InvokeAsync(object request, CancellationToken cancellationToken);
}