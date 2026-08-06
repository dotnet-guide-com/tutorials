using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace BackgroundJobQueueMinimal.Jobs;

public sealed class BoundedBackgroundJobQueue :
    IBackgroundJobQueue
{
    private readonly
        Channel<EmailJob>
        _channel;

    private int _accepting =
        1;

    public BoundedBackgroundJobQueue(
        IOptions<
            BackgroundJobQueueOptions>
            options)
    {
        ArgumentNullException
            .ThrowIfNull(
                options);

        int capacity =
            options.Value.Capacity;

        if (capacity
            <= 0)
        {
            throw new
                ArgumentOutOfRangeException(
                    nameof(options),
                    capacity,
                    "Queue capacity must be greater than zero.");
        }

        Capacity =
            capacity;

        _channel =
            Channel.CreateBounded<
                EmailJob>(
                    new
                        BoundedChannelOptions(
                            capacity)
                    {
                        FullMode =
                            BoundedChannelFullMode
                                .Wait,

                        SingleReader =
                            true,

                        SingleWriter =
                            false,

                        AllowSynchronousContinuations =
                            false
                    });
    }

    public int Capacity
    {
        get;
    }

    public int Depth =>
        _channel.Reader.Count;

    public bool IsAccepting =>
        Volatile.Read(
            ref _accepting)
        == 1;

    public ValueTask EnqueueAsync(
        EmailJob job,
        CancellationToken
            cancellationToken =
                default)
    {
        ArgumentNullException
            .ThrowIfNull(
                job);

        return _channel.Writer
            .WriteAsync(
                job,
                cancellationToken);
    }

    public async
        IAsyncEnumerable<EmailJob>
        ReadAllAsync(
            [EnumeratorCancellation]
            CancellationToken
                cancellationToken =
                    default)
    {
        await foreach (
            EmailJob job
            in _channel.Reader
                .ReadAllAsync(
                    cancellationToken))
        {
            yield return job;
        }
    }

    public void Complete()
    {
        if (Interlocked.Exchange(
                ref _accepting,
                0)
            == 0)
        {
            return;
        }

        _channel.Writer
            .TryComplete();
    }
}