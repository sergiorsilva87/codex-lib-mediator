using CodexLibMediator.Delegates;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Handlers;

internal sealed class PipelineHandlerInvoker<TRequest>(IServiceProvider serviceProvider) : IRequestHandlerInvoker
    where TRequest : IRequest
{
    public Task InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest>>()
            .ToArray();

        if (behaviors.Length == 0)
        {
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
            return handler.HandleAsync(typedRequest, cancellationToken);
        }

        RequestHandlerDelegate next = () =>
        {
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
            return handler.HandleAsync(typedRequest, cancellationToken);
        };

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentNext = next;
            next = () => behavior.HandleAsync(typedRequest, cancellationToken, currentNext);
        }

        return next();
    }
}

internal sealed class PipelineHandlerInvoker<TRequest, TResult>(IServiceProvider serviceProvider) : IRequestHandlerInvoker<TResult>
    where TRequest : IRequest<TResult>
{
    public Task<TResult> InvokeAsync(object request, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResult>>()
            .ToArray();

        if (behaviors.Length == 0)
        {
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResult>>();
            return handler.HandleAsync(typedRequest, cancellationToken);
        }

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

        return next();
    }
}