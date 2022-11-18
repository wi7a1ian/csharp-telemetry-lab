using System.Reflection;
using System.Diagnostics.Metrics;

namespace RawVsOTelPoc.Api.HostedServices;

// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/metrics/getting-started/README.md
// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/metrics/api.md
public class MetricGeneratingService : IHostedService, IDisposable
{
    private long executionCount = 0;
    private readonly ILogger<MetricGeneratingService> _logger;
    private Timer? _timer = null;

    private static readonly Meter MyMeter = new("RawVsOTelPoc.Api.MetricGeneratingService");

    private readonly Counter<long> tickCounter = MyMeter.CreateCounter<long>("fake_tick_count");
    private readonly ObservableCounter<long> asyncCounter;
    private readonly Histogram<long> randHistogram = MyMeter.CreateHistogram<long>("fake_value_size"); //  creates fake_value_size_bucket
    private readonly ObservableGauge<long> asyncGauge;
    private readonly UpDownCounter<long> upDownCounter = MyMeter.CreateUpDownCounter<long>("fake_live_sth_count");
    // private readonly ObservableUpDownCounter<long> upDownCounter = MyMeter.CreateObservableUpDownCounter<long>("fake_live_sth_count");

    public MetricGeneratingService(ILogger<MetricGeneratingService> logger) 
    {
        _logger = logger;
        asyncCounter = MyMeter.CreateObservableCounter<long>("fake_atomic_tick_count", () => Interlocked.Read(ref executionCount));
        asyncGauge = MyMeter.CreateObservableGauge<long>("fake_atomic_value_ms", () => new Random().NextInt64(100));
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(1));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        upDownCounter.Add(1, tag: new("foo", "bar"));
        var count = Interlocked.Increment(ref executionCount);
        tickCounter.Add(1, new("foo", "bar"), new("obj_name", nameof(MetricGeneratingService)));
        randHistogram.Record(new Random().NextInt64(100), tag: new("foo", "bar"));
        upDownCounter.Add(-1, tag: new("foo", "bar"));
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
        => _timer?.Dispose();
}