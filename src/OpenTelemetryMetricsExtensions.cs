using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using System.Reflection;

namespace RawVsOTelPoc.Api;

internal static class OpenTelemetryMetricsExtensions
{
    // OpenTelemetry
    private static string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

    // Switch between Zipkin/Jaeger/OTLP/Console by setting UseTracingExporter in appsettings.json.
    private static string serviceName = "AspNetCoreExampleService";

    private static  Action<ResourceBuilder> configureResource = r => r.AddService(
        serviceName, serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);
        
    public static IServiceCollection AddMetricsViaOpenTelemetry(this IServiceCollection services, IConfiguration cfg)
    {
        if(!bool.Parse(cfg["UseOT"])) return services;

        _ = services ?? throw new ArgumentNullException(nameof(services));

        return services.AddOpenTelemetryMetrics(options =>
        {
            options.ConfigureResource(configureResource)
                .AddMeter("RawVsOTelPoc.*")
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddPrometheusExporter();
        });
    }

    public static IApplicationBuilder UseMetricsViaOpenTelemetry(this IApplicationBuilder app, IConfiguration cfg)
    {
        if(!bool.Parse(cfg["UseOT"])) return app;

        return app.UseOpenTelemetryPrometheusScrapingEndpoint();
    }
}