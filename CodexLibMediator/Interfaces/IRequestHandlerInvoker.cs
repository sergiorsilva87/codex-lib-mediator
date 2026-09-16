namespace CodexLibMediator.Interfaces;

internal interface IRequestHandlerInvoker
{
    Task InvokeAsync(object request, CancellationToken cancellationToken);
}

internal interface IRequestHandlerInvoker<TResult>
{
    Task<TResult> InvokeAsync(object request, CancellationToken cancellationToken);
}