using CodexLibMediator.Delegates;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Handlers;

internal sealed class PipelineHandlerInvoker<TRequest, TResult>(IServiceProvider serviceProvider) : IRequestHandlerInvoker
    where TRequest : IRequest<TResult>
{
    public async Task<object?> InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResult>>()
            .ToArray();

        RequestHandlerDelegate<TResult> next = () =>
        {
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResult>>();
            return handler.HandleAsync(typedRequest, cancellationToken);
        };

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentNext = next;

            next = () => behavior.HandleAsync(typedRequest, cancellationToken, currentNext);
        }

        return await next();
    }
}