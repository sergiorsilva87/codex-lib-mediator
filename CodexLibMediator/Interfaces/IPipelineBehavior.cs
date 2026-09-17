using CodexLibMediator.Delegates;

namespace CodexLibMediator.Interfaces;

public interface IPipelineBehavior<in TRequest> where TRequest : IRequest
{
    Task HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate next);
}

public interface IPipelineBehavior<in TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResult> next);
}