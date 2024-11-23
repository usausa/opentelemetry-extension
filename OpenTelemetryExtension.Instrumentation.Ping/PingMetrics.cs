namespace OpenTelemetryExtension.Instrumentation.Ping;

using System.Diagnostics.Metrics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

using Microsoft.Extensions.Logging;

internal sealed class PingMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(PingMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly Target[] targets;

    private readonly Timer timer;

    public PingMetrics(
        ILogger<PingMetrics> log,
        PingOptions options)
    {
        log.InfoMetricsEnabled(nameof(PingMetrics));

        host = options.Host;

        targets = options.Target.Select(x => new Target(options.Timeout, x)).ToArray();

        MeterInstance.CreateObservableGauge("ping.result.time", Measure);

        timer = new Timer(_ => Update(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    public void Dispose()
    {
        timer.Dispose();
        foreach (var target in targets)
        {
            target.Dispose();
        }
    }

    private void Update()
    {
        foreach (var target in targets)
        {
            _ = Task.Run(async () => await target.UpdateAsync());
        }
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> Measure()
    {
        var values = new List<Measurement<double>>(targets.Length);

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var target in targets)
        {
            var time = target.RoundtripTime;
            if (time.HasValue)
            {
                values.Add(new Measurement<double>(time.Value, new("host", host), new("address", target.Setting.Address), new("name", target.Setting.Name ?? target.Setting.Address)));
            }
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // Target
    //--------------------------------------------------------------------------------

    private sealed class Target : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock sync = new();
#else
        private readonly object sync = new();
#endif

        private readonly Ping ping = new();

        private readonly int timeout;

        private readonly IPAddress address;

        public TargetEntry Setting { get; }

        public long? RoundtripTime
        {
            get
            {
                lock (sync)
                {
                    return field;
                }
            }
            private set
            {
                lock (sync)
                {
                    field = value;
                }
            }
        }

        public Target(int timeout, TargetEntry setting)
        {
            this.timeout = timeout;
            Setting = setting;
            address = Dns.GetHostAddresses(setting.Address)[0];
        }

        public void Dispose()
        {
            ping.Dispose();
        }

#pragma warning disable CA1031
        public async ValueTask UpdateAsync()
        {
            try
            {
                var result = await ping.SendPingAsync(address, timeout);
                RoundtripTime = result.Status == IPStatus.Success ? result.RoundtripTime : null;
            }
            catch
            {
                RoundtripTime = null;
            }
        }
#pragma warning restore CA1031
    }
}
