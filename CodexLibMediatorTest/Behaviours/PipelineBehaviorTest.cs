using CodexLibMediator.Delegates;
using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Behaviours;

public class PipelineBehaviorTest
{
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
    
    [Fact]
    public void PipelineBehaviors_ShouldBeResolvedInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<List<string>>();

        services.AddTransient<IPipelineBehavior<MultipleBehaviorQuery, long>, ValidationBehavior>();
        services.AddTransient<IPipelineBehavior<MultipleBehaviorQuery, long>, LoggingBehavior>();

        using var serviceProvider = services.BuildServiceProvider();
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<MultipleBehaviorQuery, long>>()
            .ToArray();

        Assert.Equal(
            [
                typeof(ValidationBehavior),
                typeof(LoggingBehavior)
            ],
            behaviors.Select(x => x.GetType()));
    }
    
    [Fact]
    public async Task SendAsync_ShouldExecuteExplicitlyConfiguredBehaviorsInRegistrationOrder()
    {
        var services = new ServiceCollection();

        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(MultipleBehaviorQueryHandler).Assembly, options =>
        {
            options.AddBehavior<ValidationBehavior>();
            options.AddBehavior<LoggingBehavior>();
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new MultipleBehaviorQuery(123));
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(123, result);
        Assert.Equal(
            [
                "Validation-Before",
                "Logging-Before",
                "Handler",
                "Logging-After",
                "Validation-After"
            ],
            execution);
    }
    
    [Fact]
    public async Task SendAsync_ShouldExecutePipelineBehaviorTwice_WhenRegisteredTwice()
    {
        var services = new ServiceCollection();
        services.AddSingleton<List<string>>();
        services.AddCodexMediator(typeof(DuplicatedBehaviorRequestHandler).Assembly, options =>
        {
            options.AddBehavior<DuplicatedBehavior>();
            options.AddBehavior<DuplicatedBehavior>();
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        DuplicatedBehavior.Reset();

        await mediator.SendAsync(new DuplicatedBehaviorRequest());
        Assert.Equal(2, DuplicatedBehavior.ExecutionCount);
    }
    
    [Fact]
    public void AddCodexMediator_ShouldIgnoreInvalidBehaviorType()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(IMediator).Assembly, options =>
        {
            options.AddBehavior<InvalidBehavior>();
        });

        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        Assert.NotNull(mediator);
    }
    
    [Fact]
    public async Task SendAsync_ShouldRegisterAutomaticallyScannedBehaviors_WhenExplicitConfigurationIsEmpty()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(EmptyConfigurationRequest).Assembly, _ => { });
        
        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        EmptyConfigurationBehavior.Reset();

        await mediator.SendAsync(new EmptyConfigurationRequest());
        Assert.Equal(1, EmptyConfigurationBehavior.ExecutionCount);
    }
    
    [Fact]
    public async Task SendAsync_ShouldExecuteOnlyExplicitlyConfiguredBehaviors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<List<string>>();
        services.AddCodexMediator( typeof(ExplicitBehaviorRequestHandler).Assembly, options =>
        {
            options.AddBehavior<ExplicitBehavior>();
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        await mediator.SendAsync(new ExplicitBehaviorRequest());
        var execution = serviceProvider.GetRequiredService<List<string>>();

        Assert.Equal(["ExplicitBehavior", "Handler" ], execution);
    }

    private sealed record UnregisteredQuery : IQuery<long>;

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
        public Task<long> HandleAsync(CancellationTokenQuery query, CancellationToken cancellationToken)
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
    
    private sealed class DuplicatedBehaviorRequest : IRequest
    {
    }

    private sealed class DuplicatedBehaviorRequestHandler : IRequestHandler<DuplicatedBehaviorRequest>
    {
        public Task HandleAsync(DuplicatedBehaviorRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class DuplicatedBehavior : IPipelineBehavior<DuplicatedBehaviorRequest>
    {
        public static int ExecutionCount { get; private set; }

        public Task HandleAsync(DuplicatedBehaviorRequest request, CancellationToken cancellationToken, 
            RequestHandlerDelegate next)
        {
            ExecutionCount++;
            return next();
        }

        public static void Reset()
        {
            ExecutionCount = 0;
        }
    }
    
    private sealed class InvalidBehavior
    {
    }
    
    private sealed class EmptyConfigurationRequest : IRequest
    {
    }

    private sealed class EmptyConfigurationRequestHandler : IRequestHandler<EmptyConfigurationRequest>
    {
        public Task HandleAsync(EmptyConfigurationRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyConfigurationBehavior : IPipelineBehavior<EmptyConfigurationRequest>
    {
        private static int _executionCount;

        public static int ExecutionCount => _executionCount;

        public static void Reset()
        {
            _executionCount = 0;
        }

        public async Task HandleAsync(EmptyConfigurationRequest request, CancellationToken cancellationToken, 
            RequestHandlerDelegate next)
        {
            _executionCount++;

            await next();
        }
    }
    
    private sealed record ExplicitBehaviorRequest : IRequest;

    private sealed class ExplicitBehaviorRequestHandler(List<string> execution) : IRequestHandler<ExplicitBehaviorRequest>
    {
        public Task HandleAsync(ExplicitBehaviorRequest request, CancellationToken cancellationToken)
        {
            execution.Add("Handler");
            return Task.CompletedTask;
        }
    }

    private sealed class ExplicitBehavior(List<string> execution) : IPipelineBehavior<ExplicitBehaviorRequest>
    {
        public async Task HandleAsync(ExplicitBehaviorRequest request, CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            execution.Add("ExplicitBehavior");
            await next();
        }
    }

    private sealed class AutomaticallyScannedBehavior(List<string> execution) : IPipelineBehavior<ExplicitBehaviorRequest>
    {
        public async Task HandleAsync(ExplicitBehaviorRequest request, CancellationToken cancellationToken, RequestHandlerDelegate next)
        {
            execution.Add("AutomaticallyScannedBehavior");
            await next();
        }
    }
}