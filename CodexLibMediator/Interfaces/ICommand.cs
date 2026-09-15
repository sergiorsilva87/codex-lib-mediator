namespace CodexLibMediator.Interfaces;

public interface ICommand : IRequest
{
}

public interface ICommand<TResult> : IRequest<TResult>
{
}