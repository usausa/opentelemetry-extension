namespace OpenTelemetryExporterService.Metrics;

using System;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Reflection;

internal sealed class ApplicationMetrics
{
    public const string InstrumentName = "Application";

    internal static readonly AssemblyName AssemblyName = typeof(ApplicationMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    public ApplicationMetrics()
    {
        MeterInstance.CreateObservableCounter("application.uptime", ObserveApplicationUptime);
    }

    private static long ObserveApplicationUptime() =>
        (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;
}
