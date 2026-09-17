using CodexLibMediator.Delegates;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Handlers;

internal sealed class StreamRequestHandlerInvoker<TRequest, TResult>(IServiceProvider serviceProvider)
    : IStreamRequestHandlerInvoker where TRequest : IStreamRequest<TResult>
{
    public object InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var behaviors = serviceProvider
            .GetServices<IStreamPipelineBehavior<TRequest, TResult>>().ToArray();

        StreamHandlerDelegate<TResult> next = token =>
        {
            var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResult>>();
            return handler.HandleAsync(typedRequest, token);
        };

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentNext = next;

            next = token => behavior.HandleAsync(typedRequest, token, currentNext);
        }

        return next(cancellationToken);
    }
}