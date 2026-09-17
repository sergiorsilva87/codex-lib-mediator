namespace CodexLibMediator.Interfaces;

public interface IStreamRequest<TResult>
{
}

public interface IStreamRequestHandler<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    IAsyncEnumerable<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
}