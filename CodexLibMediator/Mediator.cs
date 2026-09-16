using System.Collections.Concurrent;
using CodexLibMediator.Handlers;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Result), Type> PipelineInvokerTypes = new();
    private static readonly ConcurrentDictionary<Type, Type> RequestInvokerTypes = new();

    public Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = PipelineInvokerTypes.GetOrAdd((requestType, typeof(TResult)), static key =>
            typeof(PipelineHandlerInvoker<,>).MakeGenericType(key.Request, key.Result));

        var invoker = (IRequestHandlerInvoker<TResult>)serviceProvider.GetRequiredService(invokerType);
        return invoker.InvokeAsync(request, cancellationToken);
    }

    public Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = RequestInvokerTypes.GetOrAdd(requestType, static type =>
            typeof(PipelineHandlerInvoker<>).MakeGenericType(type));

        var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        return invoker.InvokeAsync(request, cancellationToken);
    }
}