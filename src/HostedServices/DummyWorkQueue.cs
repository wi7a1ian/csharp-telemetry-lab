using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace RawVsOTelPoc.Api.HostedServices;

public sealed class WorkQueueOptions
{
    public int Capacity { get; set; }
    public int MaxConcurrency { get; set; }
}

public interface IWorkQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);

    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct);
}

public sealed class DummyWorkQueue : IWorkQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public DummyWorkQueue(IOptions<WorkQueueOptions> options)
    {
        BoundedChannelOptions bco = new(options.Value.Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(bco);
    }

    public async ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem)
    {
        _ = workItem ?? throw new ArgumentNullException(nameof(workItem));
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct)
        => await _queue.Reader.ReadAsync(ct);
}