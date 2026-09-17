using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Mediator;

public class MediatorTest
{
    [Fact]
    public async Task SendAsync_ShouldExecuteQueryHandler()
    {
        var services = new ServiceCollection();

        services.AddCodexMediator(typeof(GetPlayerQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetPlayerQuery(123));
        Assert.Equal(123, result);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteCommandHandlerWithResult()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(CreatePlayerCommandHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new CreatePlayerCommand());
        Assert.Equal(123L, result);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteCommandHandlerWithoutResult()
    {
        var services = new ServiceCollection();

        services.AddCodexMediator(typeof(DeletePlayerCommandHandler).Assembly);
        await using var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.SendAsync(new DeletePlayerCommand());
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateCancellationToken()
    {
        var services = new ServiceCollection();

        services.AddCodexMediator(typeof(GetPlayerQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => mediator.SendAsync(
            new GetPlayerQuery(123), cancellationTokenSource.Token));
    }

    private sealed record GetPlayerQuery(long PlayerId) : IQuery<long>;

    private sealed class GetPlayerQueryHandler : IQueryHandler<GetPlayerQuery, long>
    {
        public Task<long> HandleAsync(GetPlayerQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed record CreatePlayerCommand : ICommand<long>;

    private sealed class CreatePlayerCommandHandler : ICommandHandler<CreatePlayerCommand, long>
    {
        public Task<long> HandleAsync(CreatePlayerCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(123L);
        }
    }

    private sealed record DeletePlayerCommand : ICommand;

    private sealed class DeletePlayerCommandHandler : ICommandHandler<DeletePlayerCommand>
    {
        public Task HandleAsync(DeletePlayerCommand command, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}