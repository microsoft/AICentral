using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AICentral;

public static class ActivitySources
{
    private static readonly ConcurrentDictionary<string, long> LongObservedValues = new();
    private static readonly ConcurrentDictionary<string, ObservableGauge<long>> LongGauges = new();

    private static readonly ConcurrentDictionary<string, UpDownCounter<int>> UpDownCounters = new();

    private static readonly ConcurrentDictionary<string, Histogram<double>> HistogramCounters = new();

    /// <summary>
    /// Create a Gauge metric that can be used to record a value that can go up and down. It will display as 'aicentral.{name}' in the metrics explorer.
    /// </summary>
    /// <param name="name">Name to suffix the metric with</param>
    /// <param name="unit">Unit of measurement</param>
    /// <param name="value">The value to record</param>
    /// <param name="tags">Any additional metadata to store alongside the value</param>
    public static void RecordGaugeMetric(string name, string unit, long value, TagList? tags = null)
    {
        var otelKey = BuildGaugeKey(name, tags);
        var otelName = $"aicentral.{name}";

        LongObservedValues.AddOrUpdate(otelKey, value, (_, _) => value);

        if (!LongGauges.TryGetValue(otelKey, out _))
        {
            var tagsAsKeyValuePairs = tags.HasValue
                ? tags.Value.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value))
                : new Dictionary<string, object?>();

            var gauge = ActivitySource.AICentralMeter.CreateObservableGauge(
                otelName,
                () => LongObservedValues.GetValueOrDefault(otelKey, 0),
                unit: $"{{{unit}}}",
                description: "",
                tags: tagsAsKeyValuePairs);

            LongGauges.TryAdd(otelKey, gauge);
        }
    }

    public static string BuildGaugeKey(string name, TagList? tags)
    {
        var joinedTagValues = string.Join('.', (tags.HasValue ? tags.Value.Select(x => x.Value?.ToString() ?? string.Empty).ToArray() : Array.Empty<string>()));
        var otelKey = $"aicentral.{name}.{joinedTagValues}";
        return otelKey;
    }

    /// <summary>
    /// Create an up-down metric that can be used to record a value that can go up and down. It will display as 'aicentral.{name}' in the metrics explorer.
    /// </summary>
    /// <param name="name">Name to suffix the metric with</param>
    /// <param name="unit">Unit of measurement</param>
    /// <param name="amount">The amount (either +ve or -ve) to move the counter by</param>
    /// <param name="tags">Any additional metadata to store alongside the value</param>
    public static void RecordUpDownCounter(string name, string unit, int amount, TagList? tags = null)
    {
        var otelName = $"aicentral.{name}";

        if (!UpDownCounters.TryGetValue(otelName, out _))
        {
            var upDownCounter =
                ActivitySource.AICentralMeter.CreateUpDownCounter<int>(otelName, $"{{{unit}}}");
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

    /// <summary>
    /// Create a histogram  metric that can be used to record values such as duration.
    /// </summary>
    /// <param name="name">Name to suffix the metric with</param>
    /// <param name="unit">Unit of measurement</param>
    /// <param name="value">The value recorded</param>
    /// <param name="tags">Any additional metadata to store alongside the value</param>
    public static void RecordHistogram(string name, string unit, double value, TagList? tags = null)
    {
        var otelName = $"aicentral.{name}";

        if (!HistogramCounters.TryGetValue(otelName, out _))
        {
            var histogram = ActivitySource.AICentralMeter.CreateHistogram<double>(
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