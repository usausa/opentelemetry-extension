namespace TelemetryService.Metrics;

using System;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Reflection;

internal sealed class ApplicationMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(ApplicationMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly ApplicationOptions options;

    public ApplicationMetrics(
        ILogger<ApplicationMetrics> log,
        ApplicationOptions options)
    {
        log.InfoMetricsEnabled(nameof(ApplicationMetrics));

        this.options = options;

        MeterInstance.CreateObservableGauge("telemetry.service.uptime", MeasureUptime);
        MeterInstance.CreateObservableGauge("telemetry.service.instrumentation", MeasureInstrumentation);
    }

    private Measurement<long> MeasureUptime() =>
        new((long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds, new KeyValuePair<string, object?>("host", options.Host));

    private Measurement<double>[] MeasureInstrumentation()
    {
        var values = new Measurement<double>[options.InstrumentationList.Length];
        for (var i = 0; i < options.InstrumentationList.Length; i++)
        {
            values[i] = new Measurement<double>(1, new("host", options.Host), new("name", options.InstrumentationList[i]));
        }

        return values;
    }
}
