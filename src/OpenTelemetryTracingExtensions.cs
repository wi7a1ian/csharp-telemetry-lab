using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using System.Reflection;
using Npgsql;

namespace RawVsOTelPoc.Api;

// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/extending-the-sdk/README.md#sampler
internal static class OpenTelemetryTracingExtensions
{
    public static IServiceCollection AddTracabilityViaOpenTelemetry(this IServiceCollection services, IConfiguration cfg)
    {
        if(!bool.Parse(cfg["UseOT"])) return services;
        
        _ = services ?? throw new ArgumentNullException(nameof(services));

        services.Configure<JaegerExporterOptions>(cfg.GetSection("Jaeger"));
        services.AddHttpClient("JaegerExporter", configureClient: (client) => client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value"));

        return services.AddOpenTelemetryTracing( builder => builder
            // Add instrumentation for frameworks and libraries:
            .AddAspNetCoreInstrumentation((options) => {
                options.Filter = httpContext =>
                {
                    // Sample filtering: do not collect telemetry from healthchecks
                    return !httpContext.Request.Path.StartsWithSegments("/health");
                }; 
                options.Enrich  = (activity, eventName, rawObject) =>
                {
                    // Sample enrichment
                    if (eventName.Equals("OnStartActivity"))
                    {
                        if (rawObject is HttpRequest httpRequest)
                        {
                            activity.SetTag("requestProtocol", httpRequest.Protocol);
                        }
                    }
                    else if (eventName.Equals("OnStopActivity"))
                    {
                        if (rawObject is HttpResponse httpResponse)
                        {
                            activity.SetTag("responseLength", httpResponse.ContentLength);
                        }
                    }
                };})
            .AddHttpClientInstrumentation()
            //.AddSqlClientInstrumentation()
            .AddNpgsql() // https://www.npgsql.org/doc/diagnostics/tracing.html
            // Listen for our activities:
            .AddSource("RawVsOTelPoc.Api.Controllers.WeatherForecastController")
            // Sample only when trace context is retrieved with sampling flag
            .SetSampler(new ParentBasedSampler(new AlwaysOffSampler()))
            // Batch activities before export:
            .AddProcessor(new BatchActivityExportProcessor(new SeqExporter()))
            // Export to Grafana Tempo via OTLP protocol
            .AddOtlpExporter(o => { o.Endpoint = new Uri(cfg["Tempo:Endpoint"]); })
            // Export to Zipkin
            .AddZipkinExporter( o => { o.Endpoint = new Uri(cfg["Zipkin:Endpoint"]); })
            // Export to Jaeger
            .AddJaegerExporter()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Assembly.GetEntryAssembly().GetName().Name))); // or env var OTEL_SERVICE_NAME
    }
}