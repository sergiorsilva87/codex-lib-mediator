using CodexLibMediator.Interfaces;

namespace CodexLibMediatorTest.Commands;

public class CommandTest
{
    [Fact]
    public void Command_ShouldImplementICommand()
    {
        var command = new DeletePlayerCommand(123);
        Assert.IsType<ICommand>(command, false);
    }

    private sealed record DeletePlayerCommand(long PlayerId) : ICommand;
}