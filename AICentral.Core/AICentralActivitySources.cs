using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace AICentral.Core;

public static class AICentralActivitySources
{
    private static readonly ConcurrentDictionary<(string metric, string host, string model, string deployment), long> LongObservedValues =
        new();

    private static readonly ConcurrentDictionary<(string metric, string host, string model, string deployment), ObservableGauge<long>>
        LongGauges = new();

    private static readonly ConcurrentDictionary<(string pipeline, string metric), Counter<long>> LongCounters = new();

    private static readonly ConcurrentDictionary<(string pipeline, string metric), Histogram<double>> HistogramCounters =
        new();

    public static void RecordGaugeMetric(string metric, string host, string deployment, string model, long value)
    {
        var key = (metric, host, model, deployment);

        LongObservedValues.AddOrUpdate(key, value, (_, _) => value);

        if (!LongGauges.TryGetValue(key, out _))
        {
            var gauge = AICentralActivitySource.AICentralMeter.CreateObservableGauge(
                $"aicentral.{host.Replace(".", "_")}.{metric}", () => LongObservedValues.GetValueOrDefault(key, 0));
            LongGauges.TryAdd(key, gauge);
        }
    }

    public static void RecordCounter(string pipeline, string metric, string unit, long count)
    {
        var key = (pipeline, metric);

        if (!LongCounters.TryGetValue(key, out _))
        {
            var guage = AICentralActivitySource.AICentralMeter.CreateCounter<long>(
                $"aicentral.{pipeline}.{metric}.count", unit);
            LongCounters.TryAdd(key, guage);
        }

        if (LongCounters.TryGetValue(key, out var counter))
        {
            counter.Add(count);
        }
    }

    public static void RecordHistogram(string pipeline, string metric, string aggregation, string unit, double value)
    {
        var key = (pipeline, $"{metric}.{aggregation}");

        if (!HistogramCounters.TryGetValue(key, out _))
        {
            var guage = AICentralActivitySource.AICentralMeter.CreateHistogram<double>(
                $"aicentral.{pipeline}.{metric}.{aggregation}", unit);
            HistogramCounters.TryAdd(key, guage);
        }

        if (HistogramCounters.TryGetValue(key, out var counter))
        {
            counter.Record(value);
        }
    }
}