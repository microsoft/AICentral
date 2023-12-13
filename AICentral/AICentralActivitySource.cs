using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Identity.Client;

namespace AICentral;

/// <summary>
/// Contains the Open Telemetry Activity source you can use to emit custom metrics and traces related to AI Central
/// </summary>
public static class AICentralActivitySource
{
    public static readonly string AICentralTelemetryName = typeof(AICentralPipeline).Assembly.GetName().Name!;

    private static readonly string AICentralMeterVersion =
        typeof(AICentralPipeline).Assembly.GetName().Version!.ToString();

    static AICentralActivitySource()
    {
        AICentralMeter = new Meter(AICentralTelemetryName, AICentralMeterVersion);
        AICentralRequestActivitySource = new ActivitySource("aicentral");
    }

    public static Meter AICentralMeter { get; }

    public static ActivitySource AICentralRequestActivitySource { get; }
}