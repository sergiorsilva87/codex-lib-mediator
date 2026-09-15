using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Handlers;

internal sealed class RequestHandlerInvoker<TRequest>(IServiceProvider serviceProvider) : IRequestHandlerInvoker
    where TRequest : IRequest
{
    public async Task<object?> InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        await handler.HandleAsync((TRequest)request, cancellationToken);
        return null;
    }
}

internal sealed class RequestHandlerInvoker<TRequest, TResult>(IServiceProvider serviceProvider) : IRequestHandlerInvoker
    where TRequest : IRequest<TResult>
{
    public async Task<object?> InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResult>>();
        return await handler.HandleAsync((TRequest)request, cancellationToken);
    }
}