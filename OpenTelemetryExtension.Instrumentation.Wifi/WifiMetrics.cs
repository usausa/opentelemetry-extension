namespace OpenTelemetryExtension.Instrumentation.Wifi;

using System.Diagnostics.Metrics;
using System.Reflection;

using ManagedNativeWifi;

using Microsoft.Extensions.Logging;

internal sealed class WifiMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(WifiMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly int signalThreshold;

    private readonly bool knownOnly;

    private readonly HashSet<string> knownAccessPoint;

    public WifiMetrics(
        ILogger<WifiMetrics> log,
        WifiOptions options)
    {
        log.InfoMetricsEnabled(nameof(WifiMetrics));

        host = options.Host;
        signalThreshold = options.SignalThreshold;
        knownOnly = options.KnownOnly;
        knownAccessPoint = options.KnownAccessPoint.Select(NormalizeAddress).ToHashSet();

        MeterInstance.CreateObservableUpDownCounter("wifi.rssi", Measure);
    }

    private static string NormalizeAddress(string address)
    {
        var value = Convert.ToUInt64(address.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal), 16);
        return $"{(value >> 48) & 0xFF:X2}:{(value >> 32) & 0xFF:X2}:{(value >> 24) & 0xFF:X2}:{(value >> 16) & 0xFF:X2}:{(value >> 8) & 0xFF:X2}:{value & 0xFF:X2}";
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private KeyValuePair<string, object?>[] MakeTags(BssNetworkPack network)
    {
        return
        [
            new("host", host),
            new("ssid", network.Ssid.ToString()),
            new("bssid", network.Bssid.ToString()),
            new("protocol", network.PhyType.ToProtocolName()),
            new("band", network.Band),
            new("channel", network.Channel)
        ];
    }

    private List<Measurement<double>> Measure()
    {
        var values = new List<Measurement<double>>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var network in NativeWifi.EnumerateBssNetworks())
        {
            if (network.SignalStrength <= signalThreshold)
            {
                continue;
            }

            if (knownOnly && !knownAccessPoint.Contains(network.Bssid.ToString()))
            {
                continue;
            }

            values.Add(new Measurement<double>(network.SignalStrength, MakeTags(network)));
        }

        return values;
    }
}
