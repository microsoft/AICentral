using System.Diagnostics.Metrics;

namespace AICentral;

/// <summary>
/// Contains the Open Telemetry Activity source you can use to emit custom metrics and traces related to AI Central
/// </summary>
public static class ActivitySource
{
    public static readonly string AICentralTelemetryName = typeof(ActivitySource).Assembly.GetName().Name!;

    private static readonly string AICentralMeterVersion =
        typeof(ActivitySource).Assembly.GetName().Version!.ToString();

    /// <summary>
    /// TODO - we should inject this into the DI Container instead of using a singleton.
    /// </summary>
    static ActivitySource()
    {
        AICentralMeter = new Meter(AICentralTelemetryName, AICentralMeterVersion);
        AICentralRequestActivitySource = new System.Diagnostics.ActivitySource(AICentralTelemetryName);
    }

    public static Meter AICentralMeter { get; }

    public static System.Diagnostics.ActivitySource AICentralRequestActivitySource { get; }
}