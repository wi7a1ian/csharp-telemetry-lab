using System.Diagnostics;

// https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-collection-walkthroughs#collect-traces-using-custom-logic
// https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-collection-walkthroughs#add-code-to-collect-the-traces
// https://jimmybogard.com/activitysource-and-listener-in-net-5/
// https://www.w3.org/TR/trace-context/#sampled-flag
internal static class ActivityListenerExtensions
{
    public static IApplicationBuilder UseActivityListenerForTracability(this IApplicationBuilder app, IConfiguration cfg)
    {
        if(!bool.Parse(cfg["UseAL"])) return app;

        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        ActivitySamplingResult SampleUsingParentId(ref ActivityCreationOptions<string> options)
        {
            return ActivitySamplingResult.AllDataAndRecorded;
        };

        ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options)
        {
            return ActivitySamplingResult.AllDataAndRecorded;
        };



        ActivitySource.AddActivityListener(new ActivityListener()
        {
            ShouldListenTo = (source) => true,
            SampleUsingParentId = SampleUsingParentId,
            Sample = Sample,
            //ActivityStarted = activity => Console.WriteLine("OrionActivityListener.Started: {0,-15} {1,-60}", activity.OperationName, activity.Id),
            //ActivityStopped = activity => Console.WriteLine("OrionActivityListener.Stopped: {0,-15} {1,-60} {2,-15}", activity.OperationName, activity.Id, activity.Duration)
            ActivityStopped = activity => Console.WriteLine($"OrionActivityListener {activity.Id} > {activity.Source.Name} > {activity.DisplayName}"),
            //Dispose = ActivitySource.DetachListener(this)
        });
        return app;
    }
}