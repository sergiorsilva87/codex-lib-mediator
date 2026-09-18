using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Extensions;

public class DependencyInjectionTest
{
    [Fact]
    public void AddCodexMediator_ShouldRegisterMediator()
    {
        var services = new ServiceCollection();

        services.AddCodexMediator(typeof(GetPlayerQueryHandler).Assembly);

        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddCodexMediator_ShouldRegisterQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(GetPlayerQueryHandler).Assembly);

        using var serviceProvider = services.BuildServiceProvider();

        var handler = serviceProvider.GetService<IQueryHandler<GetPlayerQuery, long>>();
        Assert.NotNull(handler);
    }
    
    [Fact]
    public void AddCodexMediator_ShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddCodexMediator(typeof(GetPlayerQueryHandler).Assembly));

        Assert.Equal("services", exception.ParamName);
    }
    
    [Fact]
    public void AddCodexMediator_ShouldThrowWhenAssemblyIsNull()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddCodexMediator(null!));

        Assert.Equal("assembly", exception.ParamName);
    }
    
    [Fact]
    public void AddCodexMediator_ShouldThrowWhenConfigureIsNull()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddCodexMediator(
                typeof(GetPlayerQueryHandler).Assembly,
                null!));

        Assert.Equal("configure", exception.ParamName);
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