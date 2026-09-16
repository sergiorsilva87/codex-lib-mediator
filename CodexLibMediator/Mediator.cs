using System.Collections.Concurrent;
using CodexLibMediator.Handlers;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator;

// internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
// {
//     public async Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
//     {
//         var invokerType = typeof(PipelineHandlerInvoker<,>).MakeGenericType(request.GetType(), typeof(TResult));
//         var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
//         var result = await invoker.InvokeAsync(request, cancellationToken);
//         return (TResult)result!;
//     }
//     public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
//     {
//         var invokerType = typeof(RequestHandlerInvoker<>).MakeGenericType(request.GetType());
//         var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
//         await invoker.InvokeAsync(request, cancellationToken);
//     }
// }

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Result), Type> PipelineInvokerTypes = new();
    private static readonly ConcurrentDictionary<Type, Type> RequestInvokerTypes = new();

    public async Task<TResult> SendAsync<TResult>(IRequest<TResult> request,
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = PipelineInvokerTypes.GetOrAdd((requestType, typeof(TResult)), static key =>
            typeof(PipelineHandlerInvoker<,>).MakeGenericType(key.Request, key.Result));

        var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        var result = await invoker.InvokeAsync(request, cancellationToken);

        return (TResult)result!;
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = RequestInvokerTypes.GetOrAdd(requestType, static type =>
            typeof(RequestHandlerInvoker<>).MakeGenericType(type));

        var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        await invoker.InvokeAsync(request, cancellationToken);
    }
}