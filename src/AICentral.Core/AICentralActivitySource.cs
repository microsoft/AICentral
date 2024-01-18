using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AICentral.Core;

/// <summary>
/// Contains the Open Telemetry Activity source you can use to emit custom metrics and traces related to AI Central
/// </summary>
public static class AICentralActivitySource
{
    public static readonly string AICentralTelemetryName = typeof(AICentralActivitySource).Assembly.GetName().Name!;

    private static readonly string AICentralMeterVersion =
        typeof(AICentralActivitySource).Assembly.GetName().Version!.ToString();

    static AICentralActivitySource()
    {
        AICentralMeter = new Meter(AICentralTelemetryName, AICentralMeterVersion);
        AICentralRequestActivitySource = new ActivitySource(AICentralTelemetryName);
    }

    public static Meter AICentralMeter { get; }

    public static ActivitySource AICentralRequestActivitySource { get; }
}