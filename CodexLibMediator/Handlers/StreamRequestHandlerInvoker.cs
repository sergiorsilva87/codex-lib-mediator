using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Handlers;

internal sealed class StreamRequestHandlerInvoker<TRequest, TResult>(IServiceProvider serviceProvider)
    : IStreamRequestHandlerInvoker where TRequest : IStreamRequest<TResult>
{
    public object InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResult>>();
        return handler.HandleAsync(typedRequest, cancellationToken);
    }
}