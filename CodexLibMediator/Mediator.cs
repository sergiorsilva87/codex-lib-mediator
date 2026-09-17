using System.Collections.Concurrent;
using CodexLibMediator.Handlers;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, Type> NotificationInvokerTypes = new();

    private static readonly ConcurrentDictionary<Type, Type> RequestInvokerTypes = new();
    private static readonly ConcurrentDictionary<(Type Request, Type Result), Type> PipelineInvokerTypes = new();

    private static readonly ConcurrentDictionary<(Type Request, Type Result), Type> StreamInvokerTypes = new();

    public Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var notificationType = notification.GetType();
        var invokerType = NotificationInvokerTypes.GetOrAdd(
            notificationType, static type => typeof(NotificationHandlerInvoker<>).MakeGenericType(type));

        var invoker = (INotificationHandlerInvoker)ActivatorUtilities.CreateInstance(serviceProvider, invokerType);
        return invoker.InvokeAsync(notification, cancellationToken);
    }

    public Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = RequestInvokerTypes.GetOrAdd(requestType, static type =>
            typeof(PipelineHandlerInvoker<>).MakeGenericType(type));

        var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        return invoker.InvokeAsync(request, cancellationToken);
    }

    public Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = PipelineInvokerTypes.GetOrAdd((requestType, typeof(TResult)), static key =>
            typeof(PipelineHandlerInvoker<,>).MakeGenericType(key.Request, key.Result));

        var invoker = (IRequestHandlerInvoker<TResult>)serviceProvider.GetRequiredService(invokerType);
        return invoker.InvokeAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<TResult> StreamAsync<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var invokerType = StreamInvokerTypes.GetOrAdd(
            (requestType, typeof(TResult)), static key =>
                typeof(StreamRequestHandlerInvoker<,>).MakeGenericType(key.Request, key.Result));

        var invoker = (IStreamRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        return (IAsyncEnumerable<TResult>)invoker.InvokeAsync(request, cancellationToken);
    }
}