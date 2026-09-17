namespace CodexLibMediator.Delegates;

public delegate IAsyncEnumerable<TResult> StreamHandlerDelegate<TResult>(CancellationToken cancellationToken);