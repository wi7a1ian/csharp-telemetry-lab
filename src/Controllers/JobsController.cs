using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using RawVsOTelPoc.Api.HostedServices;

namespace RawVsOTelPoc.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly ILogger<JobsController> _logger;
    private readonly IWorkQueue _workQueue;

    private static readonly Meter MyMeter = new("RawVsOTelPoc.Api.JobsController");

    public JobsController(ILogger<JobsController> logger, IWorkQueue workQueue) 
        => (_logger, _workQueue) = (logger, workQueue);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GetAsync(JobCreationRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Scheduling item on the work queue.");

        var workItemSet = Guid.NewGuid().ToString();
        Func<CancellationToken, ValueTask> workitem = async c => 
        {
            long docCount = (long)(req.DocumentCount/req.WorkitemCount);

            var counter = MyMeter.CreateCounter<long>("document_count");
            var histogram = MyMeter.CreateHistogram<long>("document_count");
            var asyncGauge = MyMeter.CreateObservableGauge<long>("fake_atomic_value_ms", () => docCount);

            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.NextInt64(10)), c);

            // This doesn't make sense without specifying workitem.
            // The metric needs to span across multiple instances of work items to make sense
            // otherwise there is no SUM of values.
            counter.Add(docCount, tag: new("workitem_type", "email-threading"));
            histogram.Record(docCount, tag: new("workitem_type", "email-threading"));

            // DONT DO THIS:
            // counter.Add(docCount, new("workitem_type", "dummy"), new("workitemset_name", workItemSet)); <--- procedurally generated name 
            // Each labelset is an additional time series that has RAM, CPU, disk, and network costs.
            // https://prometheus.io/docs/practices/instrumentation/#do-not-overuse-labels
        };

        for(int i = 0; i < req.WorkitemCount; ++i)
        {
            await _workQueue.QueueAsync(workitem);
        }
        
        return Accepted();
    }

    public record JobCreationRequest(int WorkitemCount, int DocumentCount);
}
