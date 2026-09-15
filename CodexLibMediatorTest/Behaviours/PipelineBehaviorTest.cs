using CodexLibMediator.Delegates;
using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Behaviours;

public class PipelineBehaviorTest
{
    [Fact]
    public async Task SendAsync_ShouldExecuteHandlerWithoutPipelineBehaviors()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(NoBehaviorQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new NoBehaviorQuery(123));
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(123, result);
        Assert.Equal(
            ["Handler"],
            execution);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteSinglePipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(SingleBehaviorQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new SingleBehaviorQuery(123));
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(123, result);
        Assert.Equal(
            [
                "Before",
                "Handler",
                "After"
            ],
            execution);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteMultiplePipelineBehaviorsInOrder()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(MultipleBehaviorQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new MultipleBehaviorQuery(123));
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(123, result);
        Assert.Equal(
            [
                "Logging-Before",
                "Validation-Before",
                "Handler",
                "Validation-After",
                "Logging-After"
            ],
            execution);
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateCancellationTokenToPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(CancellationTokenQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var result = await mediator.SendAsync(
            new CancellationTokenQuery(123),
            cancellationToken);

        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(123, result);
        Assert.Equal(["Behavior-Token-Received", "Handler-Token-Received"], execution);
    }

    [Fact]
    public async Task SendAsync_ShouldStopPipelineWhenBehaviorDoesNotCallNext()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(StopPipelineQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new StopPipelineQuery(123));

        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(0, result);
        Assert.Equal(["Behavior"], execution);
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateHandlerExceptionThroughPipeline()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(ExceptionQueryHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new ExceptionQuery()));
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal("Handler failed.", exception.Message);
        Assert.Equal(["Behavior-Before", "Handler"], execution);
    }
    
    [Fact]
    public async Task SendAsync_ShouldThrowWhenHandlerIsNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(UnregisteredQuery).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new UnregisteredQuery()));
    }

    private sealed record UnregisteredQuery : IQuery<long>;

    // ============================================================
    // Zero Behaviors
    // ============================================================

    private sealed record NoBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class NoBehaviorQueryHandler(List<string> execution) : IQueryHandler<NoBehaviorQuery, long>
    {
        public Task<long> HandleAsync(NoBehaviorQuery query, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.FromResult(query.PlayerId);
        }
    }

    // ============================================================
    // One Behavior
    // ============================================================

    private sealed record SingleBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class SingleBehaviorQueryHandler(List<string> execution) : IQueryHandler<SingleBehaviorQuery, long>
    {
        public Task<long> HandleAsync(SingleBehaviorQuery query, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class SingleLoggingBehavior(List<string> execution) : IPipelineBehavior<SingleBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(SingleBehaviorQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
        {
            execution.Add("Before");
            var result = await next();

            execution.Add("After");
            return result;
        }
    }

    // ============================================================
    // Multiple Behaviors
    // ============================================================

    private sealed record MultipleBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class MultipleBehaviorQueryHandler(List<string> execution) : IQueryHandler<MultipleBehaviorQuery, long>
    {
        public Task<long> HandleAsync(MultipleBehaviorQuery query, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class LoggingBehavior(List<string> execution) : IPipelineBehavior<MultipleBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(MultipleBehaviorQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
        {
            execution.Add("Logging-Before");
            var result = await next();

            execution.Add("Logging-After");
            return result;
        }
    }

    private sealed class ValidationBehavior(List<string> execution) : IPipelineBehavior<MultipleBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(MultipleBehaviorQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
        {
            execution.Add("Validation-Before");
            var result = await next();

            execution.Add("Validation-After");
            return result;
        }
    }

    private sealed record CancellationTokenQuery(long PlayerId) : IQuery<long>;

    private sealed class CancellationTokenQueryHandler(
        List<string> execution) : IQueryHandler<CancellationTokenQuery, long>
    {
        public Task<long> HandleAsync(
            CancellationTokenQuery query,
            CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                throw new InvalidOperationException(
                    "The CancellationToken was not propagated to the Handler.");

            execution.Add("Handler-Token-Received");

            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class CancellationTokenBehavior(List<string> execution) : IPipelineBehavior<CancellationTokenQuery, long>
    {
        public async Task<long> HandleAsync(CancellationTokenQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
        {
            if (cancellationToken == CancellationToken.None)
                throw new InvalidOperationException("The CancellationToken was not propagated to the Behavior.");

            execution.Add("Behavior-Token-Received");
            return await next();
        }
    }

    private sealed record StopPipelineQuery(long PlayerId) : IQuery<long>;

    private sealed class StopPipelineQueryHandler(List<string> execution) : IQueryHandler<StopPipelineQuery, long>
    {
        public Task<long> HandleAsync(StopPipelineQuery query, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class StopPipelineBehavior(List<string> execution) : IPipelineBehavior<StopPipelineQuery, long>
    {
        public Task<long> HandleAsync(StopPipelineQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
        {
            execution.Add("Behavior");
            return Task.FromResult(0L);
        }
    }

    private sealed record ExceptionQuery : IQuery<long>;

    private sealed class ExceptionQueryHandler(List<string> execution) : IQueryHandler<ExceptionQuery, long>
    {
        public Task<long> HandleAsync(ExceptionQuery query, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            throw new InvalidOperationException("Handler failed.");
        }
    }

    private sealed class ExceptionBehavior(List<string> execution) : IPipelineBehavior<ExceptionQuery, long>
    {
        public async Task<long> HandleAsync(ExceptionQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
        {
            execution.Add("Behavior-Before");
            var result = await next();

            execution.Add("Behavior-After");
            return result;
        }
    }
}