namespace OpenTelemetryExtension.Instrumentation.Wifi;

using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.Logging;

// TODO
#pragma warning disable CA1823
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
internal sealed class WifiMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(WifiMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    public WifiMetrics(
        ILogger<WifiMetrics> log,
        WifiOptions options)
    {
        log.InfoMetricsEnabled(nameof(WifiMetrics));

        host = options.Host;

        // TODO
    }

    public void Dispose()
    {
        // TODO
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    // TODO

    //--------------------------------------------------------------------------------
    // AccessPoint
    //--------------------------------------------------------------------------------

    // TODO
}
