using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ppip.BuildingBlocks.Observability;

/// <summary>
/// Cablea OpenTelemetry (traces, métricas, logs) según ADR-011: exportación
/// OTLP hacia el Collector, nunca directo a Prometheus/Loki/Tempo desde el
/// servicio (docs/13-observability/01, docs/04-architecture/08).
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Config esperada: <c>Ppip:Otel:Endpoint</c> (URL OTLP/gRPC del Collector).
    /// Si falta o está vacía, los exporters OTLP no se registran: el servicio
    /// sigue arrancando (no es una dependencia dura del perfil `app`, ver
    /// infrastructure/docker/README.md).
    /// </summary>
    public static IHostApplicationBuilder AddPpipObservability(this IHostApplicationBuilder builder, string serviceName)
    {
        var otlpEndpoint = builder.Configuration["Ppip:Otel:Endpoint"];
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);
        var endpointUri = hasOtlpEndpoint ? new Uri(otlpEndpoint!) : null;

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: ResolveServiceVersion())
            .AddAttributes([new("deployment.environment", builder.Environment.EnvironmentName)]);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            if (endpointUri is not null)
            {
                options.AddOtlpExporter(o => o.Endpoint = endpointUri);
            }
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: serviceName, serviceVersion: ResolveServiceVersion()))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (endpointUri is not null)
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = endpointUri);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (endpointUri is not null)
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = endpointUri);
                }
            });

        return builder;
    }

    private static string ResolveServiceVersion() =>
        typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
