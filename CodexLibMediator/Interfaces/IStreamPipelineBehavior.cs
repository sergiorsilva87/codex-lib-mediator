using CodexLibMediator.Delegates;

namespace CodexLibMediator.Interfaces;

public interface IStreamPipelineBehavior<in TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken, 
        StreamHandlerDelegate<TResult> next);
}