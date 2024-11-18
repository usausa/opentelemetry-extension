namespace OpenTelemetryExtension.Instrumentation.Wifi;

public sealed class AccessPointEntry
{
    public string Address { get; set; } = default!;

    public string? Name { get; set; }
}

#pragma warning disable CA1819
public sealed class WifiOptions
{
    public string Host { get; set; } = default!;

    public int SignalThreshold { get; set; } = -75;

    public bool KnownOnly { get; set; }

    public AccessPointEntry[] KnownAccessPoint { get; set; } = [];
}
#pragma warning restore CA1819
