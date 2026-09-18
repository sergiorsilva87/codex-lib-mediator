using System.Runtime.CompilerServices;
using CodexLibMediator.Delegates;
using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediatorTest.Stream;

public class StreamTest
{
    [Fact]
    public async Task StreamAsync_Should_Return_All_Results()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(TestStreamHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var request = new TestStreamRequest();
        var results = new List<int>();

        await foreach (var result in mediator.StreamAsync(request)) results.Add(result);

        Assert.Equal([1, 2, 3, 4, 5], results);
    }

    [Fact]
    public async Task StreamAsync_Should_Complete_When_Handler_Returns_No_Results()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(EmptyStreamHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var request = new EmptyStreamRequest();

        var results = new List<int>();

        await foreach (var result in mediator.StreamAsync(request)) results.Add(result);

        Assert.Empty(results);
    }

    [Fact]
    public async Task StreamAsync_Should_Propagate_CancellationToken()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(CancellationStreamHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var request = new CancellationStreamRequest();

        await foreach (var result in mediator.StreamAsync(request, cancellationToken)) break;

        Assert.Equal(cancellationToken, CancellationStreamHandler.ReceivedCancellationToken);
    }

    [Fact]
    public async Task StreamAsync_Should_Stop_When_Cancellation_Is_Requested()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(CancelableStreamHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var request = new CancelableStreamRequest();
        var results = new List<int>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var result in mediator.StreamAsync(request, cancellationTokenSource.Token))
            {
                results.Add(result);
                if (result == 3)
                    await cancellationTokenSource.CancelAsync();
            }
        });

        Assert.Equal([1, 2, 3], results);
    }

    [Fact]
    public async Task StreamAsync_Should_Propagate_Handler_Exception()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(FailingStreamHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var request = new FailingStreamRequest();
        var results = new List<int>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var result in mediator.StreamAsync(request)) results.Add(result);
        });

        Assert.Equal("Stream handler failed.", exception.Message);
        Assert.Equal([1, 2], results);
    }

    [Fact]
    public async Task StreamAsync_Should_Execute_Stream_Pipeline_Behavior()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(StreamBehaviorHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        StreamBehaviorTestBehavior.Executed = false;
        var request = new StreamBehaviorRequest();
        var results = new List<int>();

        await foreach (var result in mediator.StreamAsync(request)) results.Add(result);

        Assert.True(StreamBehaviorTestBehavior.Executed);
        Assert.Equal([1, 2, 3], results);
    }

    [Fact]
    public async Task StreamAsync_Should_Allow_Behavior_To_Transform_Results()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(StreamTransformHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var request = new StreamTransformRequest();
        var results = new List<int>();

        await foreach (var result in mediator.StreamAsync(request)) results.Add(result);

        Assert.Equal([10, 20, 30], results);
    }

    [Fact]
    public async Task StreamAsync_Should_Execute_Multiple_Behaviors_In_Order()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(MultipleStreamBehaviorsHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        MultipleStreamBehaviorEvents.Events.Clear();

        var request = new MultipleStreamBehaviorsRequest();
        var results = new List<int>();

        await foreach (var result in mediator.StreamAsync(request)) results.Add(result);

        Assert.Equal([1, 2, 3], results);
        Assert.Equal(
            [
                "Behavior1-Before",
                "Behavior2-Before",
                "Handler",
                "Behavior2-After",
                "Behavior1-After"
            ],
            MultipleStreamBehaviorEvents.Events);
    }
    
    [Fact]
    public async Task StreamAsync_ShouldThrow_WhenStreamRequestHandlerInvokerIsNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddCodexMediator(typeof(IMediator).Assembly);

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        Assert.Throws<InvalidOperationException>(() =>
            mediator.StreamAsync(new UnhandledStreamRequest()));
    }

    private sealed class TestStreamRequest : IStreamRequest<int>
    {
    }

    private sealed class TestStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 5; i++)
            {
                yield return i;
                await Task.Yield();
            }
        }
    }

    private sealed class EmptyStreamRequest : IStreamRequest<int>
    {
    }

    private sealed class EmptyStreamHandler : IStreamRequestHandler<EmptyStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(EmptyStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class CancellationStreamRequest : IStreamRequest<int>
    {
    }

    private sealed class CancellationStreamHandler : IStreamRequestHandler<CancellationStreamRequest, int>
    {
        public static CancellationToken ReceivedCancellationToken { get; private set; }

        public async IAsyncEnumerable<int> HandleAsync(CancellationStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ReceivedCancellationToken = cancellationToken;
            yield return 1;
            await Task.Yield();
        }
    }

    private sealed class CancelableStreamRequest : IStreamRequest<int>
    {
    }

    private sealed class CancelableStreamHandler : IStreamRequestHandler<CancelableStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(CancelableStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 10; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return i;
                await Task.Yield();
            }
        }
    }

    private sealed class FailingStreamRequest : IStreamRequest<int>
    {
    }

    private sealed class FailingStreamHandler : IStreamRequestHandler<FailingStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(FailingStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            yield return 2;

            await Task.Yield();
            throw new InvalidOperationException("Stream handler failed.");
        }
    }

    private sealed class StreamBehaviorRequest : IStreamRequest<int>
    {
    }

    private sealed class StreamBehaviorHandler : IStreamRequestHandler<StreamBehaviorRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(StreamBehaviorRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            yield return 2;
            yield return 3;

            await Task.CompletedTask;
        }
    }

    private sealed class StreamBehaviorTestBehavior : IStreamPipelineBehavior<StreamBehaviorRequest, int>
    {
        public static bool Executed { get; set; }

        public IAsyncEnumerable<int> HandleAsync(StreamBehaviorRequest request,
            CancellationToken cancellationToken, StreamHandlerDelegate<int> next)
        {
            Executed = true;
            return next(cancellationToken);
        }
    }

    private sealed class StreamTransformRequest : IStreamRequest<int>
    {
    }

    private sealed class StreamTransformHandler : IStreamRequestHandler<StreamTransformRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(StreamTransformRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            yield return 2;
            yield return 3;

            await Task.CompletedTask;
        }
    }

    private sealed class StreamTransformBehavior : IStreamPipelineBehavior<StreamTransformRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(StreamTransformRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken, StreamHandlerDelegate<int> next)
        {
            await foreach (var item in next(cancellationToken)) yield return item * 10;
        }
    }

    private sealed class MultipleStreamBehaviorsRequest : IStreamRequest<int>
    {
    }

    private sealed class MultipleStreamBehaviorsHandler : IStreamRequestHandler<MultipleStreamBehaviorsRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(MultipleStreamBehaviorsRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            MultipleStreamBehaviorEvents.Events.Add("Handler");

            yield return 1;
            yield return 2;
            yield return 3;

            await Task.CompletedTask;
        }
    }

    private static class MultipleStreamBehaviorEvents
    {
        public static List<string> Events { get; } = [];
    }

    private sealed class MultipleStreamBehavior1 : IStreamPipelineBehavior<MultipleStreamBehaviorsRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(MultipleStreamBehaviorsRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            StreamHandlerDelegate<int> next)
        {
            MultipleStreamBehaviorEvents.Events.Add("Behavior1-Before");
            await foreach (var item in next(cancellationToken)) yield return item;

            MultipleStreamBehaviorEvents.Events.Add("Behavior1-After");
        }
    }

    private sealed class MultipleStreamBehavior2 : IStreamPipelineBehavior<MultipleStreamBehaviorsRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(MultipleStreamBehaviorsRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            StreamHandlerDelegate<int> next)
        {
            MultipleStreamBehaviorEvents.Events.Add("Behavior2-Before");
            await foreach (var item in next(cancellationToken)) yield return item;

            MultipleStreamBehaviorEvents.Events.Add("Behavior2-After");
        }
    }
    
    private sealed class UnhandledStreamRequest : IStreamRequest<string>
    {
    }
}