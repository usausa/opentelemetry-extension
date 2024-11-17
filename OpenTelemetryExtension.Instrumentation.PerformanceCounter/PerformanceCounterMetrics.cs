namespace OpenTelemetryExtension.Instrumentation.PerformanceCounter;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.Logging;

internal sealed class PerformanceCounterMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(PerformanceCounterMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly List<PerformanceCounter> disposables = [];

    public PerformanceCounterMetrics(
        ILogger<PerformanceCounterMetrics> log,
        PerformanceCounterOptions options)
    {
        log.InfoMetricsEnabled(nameof(PerformanceCounterMetrics));

        host = options.Host;

        foreach (var entry in options.Counter)
        {
            var counters = CreateCounters(entry.Category, entry.Counter, entry.Instance).ToArray();
            foreach (var counter in counters)
            {
                counter.NextValue();
            }

            MeterInstance.CreateObservableUpDownCounter($"{options.Prefix}.{entry.Name}", () => Measure(counters));

            disposables.AddRange(counters);
        }
    }

    public void Dispose()
    {
        foreach (var counter in disposables)
        {
            counter.Dispose();
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static IEnumerable<PerformanceCounter> CreateCounters(string category, string counter, string? instance = null)
    {
        if (!String.IsNullOrEmpty(instance))
        {
            yield return new PerformanceCounter(category, counter, instance);
        }
        else
        {
            var pcc = new PerformanceCounterCategory(category);
            if (pcc.CategoryType == PerformanceCounterCategoryType.SingleInstance)
            {
                yield return new PerformanceCounter(category, counter);
            }
            else
            {
                var names = pcc.GetInstanceNames();
                Array.Sort(names);
                foreach (var name in names)
                {
                    yield return new PerformanceCounter(category, counter, name);
                }
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private Measurement<double>[] Measure(PerformanceCounter[] counters)
    {
        var measurements = new Measurement<double>[counters.Length];

        for (var i = 0; i < counters.Length; i++)
        {
            var counter = counters[i];
            if (String.IsNullOrEmpty(counter.InstanceName))
            {
                measurements[i] = new Measurement<double>(counter.NextValue(), new KeyValuePair<string, object?>("host", host));
            }
            else
            {
                measurements[i] = new Measurement<double>(counter.NextValue(), new("host", host), new("name", counter.InstanceName));
            }
        }

        return measurements;
    }
}
