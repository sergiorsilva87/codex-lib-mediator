namespace CodexLibMediator.Delegates;

public delegate Task RequestHandlerDelegate();
public delegate Task<TResult> RequestHandlerDelegate<TResult>();