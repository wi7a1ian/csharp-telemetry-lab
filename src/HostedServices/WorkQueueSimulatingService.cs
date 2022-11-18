using Microsoft.Extensions.Options;
namespace RawVsOTelPoc.Api.HostedServices;

public class WorkQueueSimulatingService : BackgroundService
{
    private readonly ILogger<WorkQueueSimulatingService> _logger;
    private readonly IWorkQueue _workQueue;
    private readonly WorkQueueOptions _options;

    public WorkQueueSimulatingService(ILogger<WorkQueueSimulatingService> logger, IWorkQueue workQueue, IOptions<WorkQueueOptions> options) 
        => (_logger, _workQueue, _options) = (logger, workQueue, options.Value);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await semaphore.WaitAsync();
                _logger.LogInformation("Executing task work item.");
                var workItem = await _workQueue.DequeueAsync(stoppingToken);
                workItem(stoppingToken).AsTask().ContinueWith( x => semaphore.Release()); // fire-and-forget
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }
}