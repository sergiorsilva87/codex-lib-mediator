using CodexLibMediator.Interfaces;

namespace CodexLibMediatorTest.Commands.Handlers;

public class CommandHandlerTest
{
    [Fact]
    public async Task CommandHandler_ShouldImplementICommandHandler()
    {
        var handler = new DeletePlayerCommandHandler();
        var command = new DeletePlayerCommand();

        await handler.HandleAsync(command, CancellationToken.None);
    }

    [Fact]
    public async Task CommandHandler_ShouldReturnResult()
    {
        var handler = new CreatePlayerCommandHandler();

        var result = await handler.HandleAsync(new CreatePlayerCommand(), CancellationToken.None);
        Assert.Equal(123L, result);
    }

    private sealed record DeletePlayerCommand : ICommand;

    private sealed record CreatePlayerCommand : ICommand<long>;

    private sealed class CreatePlayerCommandHandler : ICommandHandler<CreatePlayerCommand, long>
    {
        public Task<long> HandleAsync(CreatePlayerCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(123L);
        }
    }

    private sealed class DeletePlayerCommandHandler : ICommandHandler<DeletePlayerCommand>
    {
        public Task HandleAsync(DeletePlayerCommand command, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}