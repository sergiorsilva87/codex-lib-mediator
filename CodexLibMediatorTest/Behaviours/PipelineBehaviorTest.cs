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

    [Fact]
    public async Task SendAsync_ShouldExecuteCommandWithoutPipelineBehaviors()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(NoBehaviorCommandHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new NoBehaviorCommand());
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(["Handler"], execution);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteSinglePipelineBehaviorWithoutResult()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(SingleBehaviorCommandHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new SingleBehaviorCommand());

        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(
            [
                "Before",
                "Handler",
                "After"
            ],
            execution);
    }

    [Fact]
    public async Task SendAsync_ShouldExecuteMultiplePipelineBehaviorsWithoutResult()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(MultipleBehaviorCommandHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new MultipleBehaviorCommand());

        var execution = serviceProvider.GetRequiredService<List<string>>();

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
    public async Task SendAsync_ShouldPropagateCancellationTokenToPipelineBehaviorWithoutResult()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(CancellationTokenCommandHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await mediator.SendAsync(new CancellationTokenCommand(), cancellationToken);
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(
            [
                "Behavior-Token-Received",
                "Handler-Token-Received"
            ],
            execution);
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateHandlerExceptionThroughPipelineWithoutResult()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(ExceptionCommandHandler).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new ExceptionCommand()));

        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal("Handler failed.", exception.Message);
        Assert.Equal(["Behavior-Before", "Handler"], execution);
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

    private sealed record NoBehaviorCommand : ICommand;

    private sealed class NoBehaviorCommandHandler(List<string> execution) : ICommandHandler<NoBehaviorCommand>
    {
        public Task HandleAsync(NoBehaviorCommand command, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.CompletedTask;
        }
    }

    private sealed record SingleBehaviorCommand : ICommand;

    private sealed class SingleBehaviorCommandHandler(List<string> execution) : ICommandHandler<SingleBehaviorCommand>
    {
        public Task HandleAsync(SingleBehaviorCommand command, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.CompletedTask;
        }
    }

    private sealed class SingleCommandBehavior(List<string> execution) : IPipelineBehavior<SingleBehaviorCommand>
    {
        public async Task HandleAsync(SingleBehaviorCommand request,
            CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            execution.Add("Before");
            await next();
            execution.Add("After");
        }
    }


    private sealed record MultipleBehaviorCommand : ICommand;

    private sealed class MultipleBehaviorCommandHandler(List<string> execution) :
        ICommandHandler<MultipleBehaviorCommand>
    {
        public Task HandleAsync(MultipleBehaviorCommand command, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.CompletedTask;
        }
    }

    private sealed class LoggingCommandBehavior(List<string> execution) : IPipelineBehavior<MultipleBehaviorCommand>
    {
        public async Task HandleAsync(MultipleBehaviorCommand request,
            CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            execution.Add("Logging-Before");
            await next();
            execution.Add("Logging-After");
        }
    }

    private sealed class ValidationCommandBehavior(List<string> execution) : IPipelineBehavior<MultipleBehaviorCommand>
    {
        public async Task HandleAsync(MultipleBehaviorCommand request,
            CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            execution.Add("Validation-Before");
            await next();
            execution.Add("Validation-After");
        }
    }

    private sealed record CancellationTokenCommand : ICommand;

    private sealed class CancellationTokenCommandHandler(List<string> execution) : ICommandHandler<CancellationTokenCommand>
    {
        public Task HandleAsync(CancellationTokenCommand command, CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                throw new InvalidOperationException(
                    "The CancellationToken was not propagated to the Handler.");

            execution.Add("Handler-Token-Received");
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationTokenCommandBehavior(List<string> execution) : IPipelineBehavior<CancellationTokenCommand>
    {
        public async Task HandleAsync(CancellationTokenCommand request,
            CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            if (cancellationToken == CancellationToken.None)
                throw new InvalidOperationException(
                    "The CancellationToken was not propagated to the Behavior.");

            execution.Add("Behavior-Token-Received");
            await next();
        }
    }

    private sealed record ExceptionCommand : ICommand;

    private sealed class ExceptionCommandHandler(List<string> execution) : ICommandHandler<ExceptionCommand>
    {
        public Task HandleAsync(ExceptionCommand command, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            throw new InvalidOperationException("Handler failed.");
        }
    }

    private sealed class ExceptionCommandBehavior(List<string> execution) : IPipelineBehavior<ExceptionCommand>
    {
        public async Task HandleAsync(ExceptionCommand request,
            CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            execution.Add("Behavior-Before");
            await next();
            execution.Add("Behavior-After");
        }
    }
}