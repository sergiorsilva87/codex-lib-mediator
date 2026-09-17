using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Queries.Handlers;

public class QueryHandlerTest
{
    [Fact]
    public async Task QueryHandler_ShouldReturnExpectedResult()
    {
        var handler = new GetPlayerQueryHandler();
        var query = new GetPlayerQuery();

        var result = await handler.HandleAsync(query, CancellationToken.None);
        Assert.Equal(123L, result);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(GetPlayerQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new GetPlayerQuery());
        Assert.Equal(123, result);
    }

    private sealed record GetPlayerQuery : IQuery<long>;

    private sealed class GetPlayerQueryHandler : IQueryHandler<GetPlayerQuery, long>
    {
        public Task<long> HandleAsync(GetPlayerQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(123L);
        }
    }
}