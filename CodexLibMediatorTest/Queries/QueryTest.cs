using CodexLibMediator.Interfaces;

namespace CodexLibMediatorTest.Queries;

public class QueryTest
{
    [Fact]
    public void Query_ShouldImplementIQueryWithResult()
    {
        var query = new GetPlayerQuery(123);
        Assert.IsType<IQuery<PlayerDto>>(query, false);
    }

    private sealed record PlayerDto;

    private sealed record GetPlayerQuery(long PlayerId) : IQuery<PlayerDto>;
}