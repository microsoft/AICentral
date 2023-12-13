using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace AICentral;

public static class AICentralActivitySources
{
    private static readonly ConcurrentDictionary<(string metric, string host, string model), long> LongObservedValues =
        new();

    private static readonly ConcurrentDictionary<(string metric, string host, string model), ObservableGauge<long>>
        LongGauges = new();

    private static readonly ConcurrentDictionary<(string pipeline, string metric), UpDownCounter<long>>
        LongUpDowns = new();

    private static readonly ConcurrentDictionary<(string pipeline, string metric), Counter<long>> LongCounters = new();

    private static readonly ConcurrentDictionary<(string pipeline, string metric), Histogram<double>> HistogramCounters =
        new();

    public static void RecordGaugeMetric(string metric, string host, string model, long value)
    {
        var key = (metric, host, model);

        LongObservedValues.AddOrUpdate(key, value, (_, _) => value);

        if (!LongGauges.TryGetValue(key, out _))
        {
            var gauge = AICentralActivitySource.AICentralMeter.CreateObservableGauge(
                $"aicentral.{metric}.{host.Replace(".", "_")}.{model}", () => LongObservedValues.GetValueOrDefault(key, 0));
            LongGauges.TryAdd(key, gauge);
        }
    }

    public static void RecordCounter(string pipeline, string metric, string unit, long count)
    {
        var key = (pipeline, string.Empty);

        if (!LongCounters.TryGetValue(key, out _))
        {
            var guage = AICentralActivitySource.AICentralMeter.CreateCounter<long>(
                $"aicentral.{pipeline}", unit);
            LongCounters.TryAdd(key, guage);
        }

        if (LongCounters.TryGetValue(key, out var counter))
        {
            counter.Add(count,
                new KeyValuePair<string, object?>("aic.metric", metric),
                new KeyValuePair<string, object?>("aic.pipeline", pipeline)
            );
        }
    }

    public static void RecordHistogram(string pipeline, string metric, string unit, double value)
    {
        var key = (pipeline, string.Empty);

        if (!HistogramCounters.TryGetValue(key, out _))
        {
            var guage = AICentralActivitySource.AICentralMeter.CreateHistogram<double>(
                $"aicentral.{pipeline}", unit);
            HistogramCounters.TryAdd(key, guage);
        }

        if (HistogramCounters.TryGetValue(key, out var counter))
        {
            counter.Record(value,
                new KeyValuePair<string, object?>("aic.metric", metric),
                new KeyValuePair<string, object?>("aic.pipeline", pipeline));
        }
    }
}