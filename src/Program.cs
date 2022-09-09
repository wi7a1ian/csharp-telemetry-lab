using RawVsOTelPoc.Api;

// Materials:
// https://devblogs.microsoft.com/dotnet/opentelemetry-net-reaches-v1-0/
// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/sdk.md
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Api/README.md#introduction-to-opentelemetry-net-tracing-api
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.Http
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.SqlClient
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry/DiagnosticSourceInstrumentation
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/DiagnosticSourceInstrumentation/DiagnosticSourceSubscriber.cs
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/DiagnosticSourceInstrumentation/DiagnosticSourceListener.cs
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.Http/Implementation/HttpHandlerDiagnosticListener.cs
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/AspNetCore
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/getting-started
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/customizing-the-sdk
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#exporter
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#special-case--instrumentation-for-libraries-producing-legacy-activity
// https://github.com/open-telemetry/opentelemetry-dotnet-contrib
// https://opentelemetry.io/docs/instrumentation/net/exporters/
// https://jimmybogard.com/building-end-to-end-diagnostics-opentelemetry-integration/
//    DiagnosticSourceSubscriber, which is the main helper for bridging diagnostic events and activities
//    https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md#discovery-of-diagnosticlisteners
// https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-collection-walkthroughs#collect-traces-using-custom-logic
// https://jimmybogard.com/activitysource-and-listener-in-net-5/
// https://github.com/grafana/tempo/tree/main/example/docker-compose
// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime

// dotnet add package OpenTelemetry --prerelease
// dotnet add package OpenTelemetry.Extensions.Hosting --prerelease
// dotnet add package OpenTelemetry.Exporter.Console --prerelease
// dotnet add package OpenTelemetry.Instrumentation.AspNetCore --prerelease
// dotnet add package OpenTelemetry.Instrumentation.Http --prerelease
// dotnet add package OpenTelemetry.Instrumentation.SqlClient --prerelease
// dotnet add package OpenTelemetry.Exporter.Jaeger
// dotnet add package OpenTelemetry.Exporter.Zipkin
// dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
// dotnet add package Npgsql.OpenTelemetry
// dotnet add package Dapper
// dotnet add package Npgsql
// dotnet add package OpenTelemetry.Instrumentation.Runtime
// DO NOT INSTALL FOR ASP.NET!! dotnet add package OpenTelemetry.Exporter.Prometheus --prerelease
// dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore --prerelease

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTracabilityViaOpenTelemetry(builder.Configuration);
builder.Services.AddMetricsViaOpenTelemetry(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<MetricGeneratingService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseActivityListenerForTracability(builder.Configuration);
app.UseMetricsViaOpenTelemetry(builder.Configuration);

app.Run();

/*
Without OpenTelemetry Instrumentation:
OrionActivityListener 00-b9a6a3f53199e6c9ece5cc3fdf6c861a-aa12d58705d69d4b-01 >  > System.Net.Http.HttpRequestOut
OrionActivityListener 00-b9a6a3f53199e6c9ece5cc3fdf6c861a-220f1f45697ead3c-01 >  > System.Net.Http.HttpRequestOut
OrionActivityListener 00-b9a6a3f53199e6c9ece5cc3fdf6c861a-5d9e8de9492cc900-01 > RawVsOTelPoc.Api.Controllers.WeatherForecastController > GetAsync
OrionActivityListener 00-b9a6a3f53199e6c9ece5cc3fdf6c861a-044c4594adb056bd-01 > Microsoft.AspNetCore > Microsoft.AspNetCore.Hosting.HttpRequestIn

With OpenTelemetry Instrumentation:
OrionActivityListener 00-559dc21a3f10b5fee2d497008fa1c947-3dcf7270551929f1-01 > OpenTelemetry.Instrumentation.Http > HTTP GET
OrionActivityListener 00-559dc21a3f10b5fee2d497008fa1c947-54c036ca586e5aae-01 > OpenTelemetry.Instrumentation.Http > HTTP GET
OrionActivityListener 00-559dc21a3f10b5fee2d497008fa1c947-b3c967214afdcfe6-01 > RawVsOTelPoc.Api.Controllers.WeatherForecastController > GetAsync
OrionActivityListener 00-559dc21a3f10b5fee2d497008fa1c947-826fb1a477d45884-01 > OpenTelemetry.Instrumentation.AspNetCore > WeatherForecast

With OpenTelemetry Instrumentation and Exporter (buffered):
SeqExporter           00-82ddd49d66d0c7afde86af32883e2dc7-5c712210f63bc1ee-01 > OpenTelemetry.Instrumentation.AspNetCore > /
SeqExporter           00-559dc21a3f10b5fee2d497008fa1c947-3dcf7270551929f1-01 > OpenTelemetry.Instrumentation.Http > HTTP GET
SeqExporter           00-559dc21a3f10b5fee2d497008fa1c947-54c036ca586e5aae-01 > OpenTelemetry.Instrumentation.Http > HTTP GET
SeqExporter           00-559dc21a3f10b5fee2d497008fa1c947-b3c967214afdcfe6-01 > RawVsOTelPoc.Api.Controllers.WeatherForecastController > GetAsync
SeqExporter           00-559dc21a3f10b5fee2d497008fa1c947-826fb1a477d45884-01 > OpenTelemetry.Instrumentation.AspNetCore > WeatherForecast
*/