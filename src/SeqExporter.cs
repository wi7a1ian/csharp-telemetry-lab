using System.Diagnostics;
using System.Text;
using OpenTelemetry;

namespace RawVsOTelPoc.Api;

// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#exporter
internal class SeqExporter : BaseExporter<Activity>
{
    private readonly string name;

    public SeqExporter(string name = "SeqExporter")
        => this.name = name;

    public override ExportResult Export(in Batch<Activity> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin(); // prevent generating telemetry

        var sb = new StringBuilder();
        foreach (var activity in batch)
        {
            // if (sb.Length > 0)
            // {
            //     sb.Append(", ");
            // }
            // sb.AppendLine($"{activity.Id} > {activity.Source.Name} > {activity.DisplayName}");
            // sb.AppendLine($"{activity.DisplayName} ({string.Join(";", activity.TagObjects.Select((k,v) => k))})");
            Console.WriteLine($"SeqExporter           {activity.Id} > {activity.Source.Name} > {activity.DisplayName}");
        }

        //Console.WriteLine($"{this.name}.Export([{sb.ToString()}])");
        return ExportResult.Success;
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        Console.WriteLine($"{this.name}.OnShutdown(timeoutMilliseconds={timeoutMilliseconds})");
        return true;
    }

    protected override void Dispose(bool disposing)
        => Console.WriteLine($"{this.name}.Dispose({disposing})");
}