using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CodexLibMediator.Delegates;
using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorBenchmark;

[MemoryDiagnoser]
public class MediatorBenchmark
{
    private IMediator _mediator = null!;

    private NoBehaviorQuery _noBehaviorQuery = null!;
    private OneBehaviorQuery _oneBehaviorQuery = null!;
    private TwoBehaviorQuery _twoBehaviorQuery = null!;
    private FiveBehaviorQuery _fiveBehaviorQuery = null!;
    
    private IServiceProvider _serviceProvider = null!;
    private Type _invokerDefinition = null!;
    private Type _invokerType = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(NoBehaviorQueryHandler).Assembly);

        var serviceProvider = services.BuildServiceProvider();

        _serviceProvider = serviceProvider;
        _mediator = serviceProvider.GetRequiredService<IMediator>();

        const string name = "CodexLibMediator.Handlers.PipelineHandlerInvoker`2";
        _invokerType = typeof(IMediator).Assembly.GetType(name)!
            .MakeGenericType(typeof(OneBehaviorQuery), typeof(long));
        
        _invokerDefinition = typeof(IMediator).Assembly.GetType(name)!;
        
        _noBehaviorQuery = new NoBehaviorQuery(123L);
        _oneBehaviorQuery = new OneBehaviorQuery(123L);
        _twoBehaviorQuery = new TwoBehaviorQuery(123L);
        _fiveBehaviorQuery = new FiveBehaviorQuery(123L);
    }

    [Benchmark(Baseline = true)]
    public async Task<long> SendAsync()
    {
        return await _mediator.SendAsync(_noBehaviorQuery);
    }

    [Benchmark]
    public async Task<long> SendAsyncWithOneBehavior()
    {
        return await _mediator.SendAsync(_oneBehaviorQuery);
    }

    [Benchmark]
    public async Task<long> SendAsyncWithTwoBehaviors()
    {
        return await _mediator.SendAsync(_twoBehaviorQuery);
    }

    [Benchmark]
    public async Task<long> SendAsyncWithFiveBehaviors()
    {
        return await _mediator.SendAsync(_fiveBehaviorQuery);
    }
    
    [Benchmark]
    public void GetOneBehavior()
    {
        _serviceProvider.GetServices<IPipelineBehavior<OneBehaviorQuery, long>>();
    }

    [Benchmark]
    public void GetOneBehaviorToArray()
    {
#pragma warning disable CA1806
        _serviceProvider.GetServices<IPipelineBehavior<OneBehaviorQuery, long>>()
#pragma warning restore CA1806
            .ToArray();
    }
    
    [Benchmark]
    public void BuildPipelineOneBehavior()
    {
        RequestHandlerDelegate<long> next = () => Task.FromResult(123L);

        var behavior = _serviceProvider.GetRequiredService<IPipelineBehavior<OneBehaviorQuery, long>>();
        var currentNext = next;

        next = () => behavior.HandleAsync( _oneBehaviorQuery, CancellationToken.None, currentNext);
    }

    [Benchmark]
    public void BuildPipelineFiveBehaviors()
    {
        RequestHandlerDelegate<long> next = () => Task.FromResult(123L);

        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<FiveBehaviorQuery, long>>()
            .ToArray();

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentNext = next;

            next = () => behavior.HandleAsync(
                _fiveBehaviorQuery,
                CancellationToken.None,
                currentNext);
        }
    }
    
    [Benchmark]
    public object ResolveInvoker()
    {
        return _serviceProvider.GetRequiredService(_invokerType);
    }
    
    [Benchmark]
    public Type MakeGenericInvokerType()
    {
        return _invokerDefinition.MakeGenericType(typeof(OneBehaviorQuery), typeof(long));
    }
    
    [Benchmark]
    public void GetNoBehavior()
    {
        _serviceProvider.GetServices<IPipelineBehavior<NoBehaviorQuery, long>>();
    }
    
    [Benchmark]
    public void GetNoBehaviorToArray()
    {
#pragma warning disable CA1806
        _serviceProvider
            .GetServices<IPipelineBehavior<NoBehaviorQuery, long>>()
#pragma warning restore CA1806
            .ToArray();
    }
    
    [Benchmark]
    public object ResolveNoBehaviorHandler()
    {
        return _serviceProvider.GetRequiredService<IRequestHandler<NoBehaviorQuery, long>>();
    }
    
    [Benchmark]
    public async Task<long> DirectHandler()
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<NoBehaviorQuery, long>>();
        return await handler.HandleAsync(_noBehaviorQuery, CancellationToken.None);
    }
    
    [Benchmark]
    public async Task<object?> InvokePipelineDirectly()
    {
        var typedRequest = _noBehaviorQuery;

        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<NoBehaviorQuery, long>>()
            .ToArray();

        RequestHandlerDelegate<long> next = () =>
        {
            var handler = _serviceProvider
                .GetRequiredService<IRequestHandler<NoBehaviorQuery, long>>();

            return handler.HandleAsync(
                typedRequest,
                CancellationToken.None);
        };

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentNext = next;

            next = () => behavior.HandleAsync(
                typedRequest,
                CancellationToken.None,
                currentNext);
        }

        return await next();
    }
    
    
    [Benchmark]
    public async Task<object?> DirectHandlerAsObject()
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<NoBehaviorQuery, long>>();

        return await handler.HandleAsync(_noBehaviorQuery, CancellationToken.None);
    }


    // =================================================================================================================
    // -- 0 BEHAVIORS
    // =================================================================================================================

    private sealed record NoBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class NoBehaviorQueryHandler : IQueryHandler<NoBehaviorQuery, long>
    {
        public Task<long> HandleAsync(NoBehaviorQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(query.PlayerId);
        }
    }


    // =================================================================================================================
    // -- 1 BEHAVIOR
    // =================================================================================================================

    private sealed record OneBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class OneBehaviorQueryHandler : IQueryHandler<OneBehaviorQuery, long>
    {
        public Task<long> HandleAsync(OneBehaviorQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class OneBehavior : IPipelineBehavior<OneBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(OneBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }


    // =================================================================================================================
    // -- 2 BEHAVIORS
    // =================================================================================================================

    private sealed record TwoBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class TwoBehaviorQueryHandler : IQueryHandler<TwoBehaviorQuery, long>
    {
        public Task<long> HandleAsync(TwoBehaviorQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class FirstBehavior : IPipelineBehavior<TwoBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(TwoBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }

    private sealed class SecondBehavior : IPipelineBehavior<TwoBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(TwoBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }


    // =================================================================================================================
    // -- 5 BEHAVIORS
    // =================================================================================================================

    private sealed record FiveBehaviorQuery(long PlayerId) : IQuery<long>;

    private sealed class FiveBehaviorQueryHandler : IQueryHandler<FiveBehaviorQuery, long>
    {
        public Task<long> HandleAsync(FiveBehaviorQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(query.PlayerId);
        }
    }

    private sealed class Behavior1 : IPipelineBehavior<FiveBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(FiveBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }

    private sealed class Behavior2 : IPipelineBehavior<FiveBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(FiveBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }

    private sealed class Behavior3 : IPipelineBehavior<FiveBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(FiveBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }

    private sealed class Behavior4 : IPipelineBehavior<FiveBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(FiveBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }

    private sealed class Behavior5 : IPipelineBehavior<FiveBehaviorQuery, long>
    {
        public async Task<long> HandleAsync(FiveBehaviorQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<long> next)
        {
            return await next();
        }
    }
}

internal static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<MediatorBenchmark>();
    }
}