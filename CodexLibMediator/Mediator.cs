using CodexLibMediator.Handlers;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var invokerType = typeof(PipelineHandlerInvoker<,>).MakeGenericType(request.GetType(), typeof(TResult));
        var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        var result = await invoker.InvokeAsync(request, cancellationToken);
        return (TResult)result!;
    }
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var invokerType = typeof(RequestHandlerInvoker<>).MakeGenericType(request.GetType());
        var invoker = (IRequestHandlerInvoker)serviceProvider.GetRequiredService(invokerType);
        await invoker.InvokeAsync(request, cancellationToken);
    }
}