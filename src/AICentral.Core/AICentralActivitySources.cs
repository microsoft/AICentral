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

    public static void RecordGaugeMetric(string name, string unit, long value, TagList? tagList = null)
    {
        var key = $"aicentral.{name}";

        LongObservedValues.AddOrUpdate(key, value, (_, _) => value);

        if (!LongGauges.TryGetValue(key, out _))
        {
            var gauge = AICentralActivitySource.AICentralMeter.CreateObservableGauge(
                key,
                () => LongObservedValues.GetValueOrDefault(key, 0),
                unit: $"{{{unit}}}");

            LongGauges.TryAdd(key, gauge);
        }
    }

    public static void RecordUpDownCounter(string name, string unit, int amount, TagList? tags = null)
    {
        var key = $"aicentral.{name}";

        if (!UpDownCounters.TryGetValue(key, out _))
        {
            var upDownCounter = AICentralActivitySource.AICentralMeter.CreateUpDownCounter<int>(key, $"{{{unit}}}");
            UpDownCounters.TryAdd(key, upDownCounter);
        }

        if (UpDownCounters.TryGetValue(key, out var counter))
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
        var key = $"aicentral.{name}";

        if (!HistogramCounters.TryGetValue(key, out _))
        {
            var guage = AICentralActivitySource.AICentralMeter.CreateHistogram<double>($"aicentral.{key}",
                $"{{{unit}}}");
            HistogramCounters.TryAdd(key, guage);
        }

        if (HistogramCounters.TryGetValue(key, out var counter))
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