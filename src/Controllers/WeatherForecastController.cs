using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Npgsql;
using Dapper;

namespace RawVsOTelPoc.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
    };

    private static readonly HttpClient client = new HttpClient();
    static readonly ActivitySource asrc = new ActivitySource(name: "RawVsOTelPoc.Api.Controllers.WeatherForecastController", version: "1.0.0");
    private readonly ILogger<WeatherForecastController> logger;
    private readonly IConfiguration cfg;

    public WeatherForecastController(IConfiguration cfg, ILogger<WeatherForecastController> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        
        SetupDatabase(cfg);
    }

    private static void SetupDatabase(IConfiguration cfg)
    {
        using var connection = new NpgsqlConnection(cfg.GetConnectionString("DefaultConnection"));
        connection.Execute("CREATE TABLE IF NOT EXISTS public.logs (id BIGSERIAL NOT NULL, body VARCHAR(100), PRIMARY KEY (id));");
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> GetAsync()
    {
        var originalActivity = Activity.Current;
        using Activity? activity = asrc.StartActivity("GetAsync");

        // We only want to collect data when request is traced, 
        // hence we should allow creation of new `activity` only if `traceparent` HTTP header was sent.
        bool wasActivityCreated = CheckIfActivityCouldBeStarted(originalActivity, activity);

        activity?.AddEvent(new ActivityEvent("Enhancing activity"));
        AddCustomTags(activity, wasActivityCreated);
        LinkToExternalTrace(activity);

        activity?.AddEvent(new ActivityEvent("making some external requests"));
        await MakeHttpCallAsync(activity);
        await MakeDbCallAsync(activity);

        activity?.AddEvent(new ActivityEvent("Generating response"));
        return GenerateResult();
    }

    private static bool CheckIfActivityCouldBeStarted(Activity? originalActivity, Activity? activity)
    {
        var wasActivityCreated = (activity != null);
        var wasTraceparentSent = (originalActivity.ParentId != null && originalActivity.Parent == null);
        var hasCurrentActivitySwitched = (originalActivity?.Id != Activity.Current?.Id);
        Debug.Assert(!wasActivityCreated || (wasTraceparentSent && hasCurrentActivitySwitched));
        return wasActivityCreated;
    }

    private static void AddCustomTags(Activity? activity, bool wasActivityCreated)
    {
        if (wasActivityCreated && activity.IsAllDataRequested == true)
        {
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });
            //activity?.AddTag() DO NOT USE, not part of OT standard
            //activity?.SetCustomProperty() DO NOT USE, not part of OT standard
        }
    }

    private static void LinkToExternalTrace(Activity? activity)
    {
        activity?.AddEvent(new ActivityEvent("Checkign activity links"));
        {
            var linkedContext = new ActivityContext(
                ActivityTraceId.CreateFromString("4bf92f3577b34da6a3ce929d0e0e4736"),
                ActivitySpanId.CreateFromString("00f067aa0ba902b7"),
                ActivityTraceFlags.None);
            var activityLinks = new List<ActivityLink> { new ActivityLink(linkedContext) };
            using var activityWithLinks = asrc.StartActivity("ActivityWithLinks", ActivityKind.Server, default(ActivityContext), null, activityLinks);
        }
    }

    private static async Task MakeHttpCallAsync(Activity? activity)
    {
        activity?.AddEvent(new ActivityEvent("Querying Google"));
        var res = await client.GetStringAsync("http://google.com");
    }

    private async Task MakeDbCallAsync(Activity? activity)
    {
        using var connection = new NpgsqlConnection(cfg.GetConnectionString("DefaultConnection"));
        await connection.ExecuteAsync("INSERT INTO public.logs (body) VALUES (@MsgBody)", new { MsgBody = activity?.Id });
    }

    private static IEnumerable<WeatherForecast> GenerateResult()
    {
        var rng = new Random();
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = rng.Next(-20, 55),
            Summary = Summaries[rng.Next(Summaries.Length)],
        });
    }
}
