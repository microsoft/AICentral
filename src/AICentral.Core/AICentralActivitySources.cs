using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AICentral.Core;

public static class AICentralActivitySources
{
    private static readonly ConcurrentDictionary<string, long> LongObservedValues = new();
    private static readonly ConcurrentDictionary<string, ObservableGauge<long>> LongGauges = new();

    private static readonly ConcurrentDictionary<string, UpDownCounter<int>> UpDownCounters = new();

    private static readonly ConcurrentDictionary<string, Histogram<double>> HistogramCounters = new();

    public static void RecordUpDownCounter(string name, string unit, int amount, TagList? tags = null)
    {
        var otelName = $"aicentral.{name}";

        if (!UpDownCounters.TryGetValue(otelName, out _))
        {
            var upDownCounter =
                AICentralActivitySource.AICentralMeter.CreateUpDownCounter<int>(otelName, $"{{{unit}}}");
            UpDownCounters.TryAdd(otelName, upDownCounter);
        }

        if (UpDownCounters.TryGetValue(otelName, out var counter))
        {
            if (tags != null)
            {
                counter.Add(amount, tags.Value);
            }
            else
            {
                counter.Add(amount);
            }
        }
    }

    public static void RecordHistogram(string name, string unit, double value, TagList? tags = null)
    {
        var otelName = $"aicentral.{name}";

        if (!HistogramCounters.TryGetValue(otelName, out _))
        {
            var histogram = AICentralActivitySource.AICentralMeter.CreateHistogram<double>(
                otelName,
                $"{{{unit}}}",
                string.Empty);

            HistogramCounters.TryAdd(otelName, histogram);
        }

        if (HistogramCounters.TryGetValue(otelName, out var counter))
        {
            if (tags != null)
            {
                counter.Record(value, tags.Value);
            }
            else
            {
                counter.Record(value);
            }
        }
    }
}