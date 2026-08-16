using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CinePick.Api.Observability;

internal static class ObservabilityExtensions
{
    private const string ServiceName = "CinePick.Api";

    public static IServiceCollection AddCinePickObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEnabled = configuration.GetValue<bool>("OpenTelemetry:Otlp:Enabled");

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                })
                    .AddHttpClientInstrumentation();

                if (otlpEnabled)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (otlpEnabled)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
