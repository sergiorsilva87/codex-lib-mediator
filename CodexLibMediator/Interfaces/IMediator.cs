namespace CodexLibMediator.Interfaces;

public interface IMediator
{
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
    
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);
    Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default);
    
}