using System.Runtime.CompilerServices;
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
            await foreach (var result in mediator.StreamAsync(request))
            {
                results.Add(result);
            }
        });

        Assert.Equal("Stream handler failed.", exception.Message);
        Assert.Equal([1, 2], results);
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
}