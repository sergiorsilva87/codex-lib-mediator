namespace CodexLibMediator.Interfaces;

internal interface IRequestHandlerInvoker
{
    Task<object?> InvokeAsync(object request, CancellationToken cancellationToken);
}