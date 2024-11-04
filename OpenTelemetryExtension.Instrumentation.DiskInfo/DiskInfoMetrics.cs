namespace OpenTelemetryExtension.Instrumentation.DiskInfo;

using System.Diagnostics.Metrics;
using System.Reflection;

using HardwareInfo.Disk;

using Microsoft.Extensions.Logging;

internal sealed class DiskInfoMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(DiskInfoMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly IDiskInfo[] disks;

    private readonly Timer timer;

    public DiskInfoMetrics(
        ILogger<DiskInfoMetrics> log,
        DiskInfoOptions options)
    {
        log.InfoMetricsEnabled(nameof(DiskInfoMetrics));

        host = options.Host ?? Environment.MachineName;

        disks = DiskInfo.GetInformation();

        var nvme = disks.Count(static x => x.DiskType == DiskType.Nvme);
        // ReSharper disable StringLiteralTypo
        MeterInstance.CreateObservableUpDownCounter("smart.powercycle", () => GatherMeasurementNvme(nvme, static x => x.PowerCycle));
        MeterInstance.CreateObservableUpDownCounter("smart.poweronhours", () => GatherMeasurementNvme(nvme, static x => x.PowerOnHours));
        // ReSharper restore StringLiteralTypo

        // TODO

        timer = new Timer(Update, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    public void Dispose()
    {
        timer.Dispose();

        foreach (var disk in disks)
        {
            disk.Dispose();
        }
    }

    private void Update(object? state)
    {
        lock (disks)
        {
            foreach (var disk in disks)
            {
                disk.Smart.Update();
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Shared
    //--------------------------------------------------------------------------------

    private KeyValuePair<string, object?>[] MakeTags(int index, string name) =>
        [new("host", host), new("name", name), new("index", index)];

    //--------------------------------------------------------------------------------
    // NVMe
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> GatherMeasurementNvme(int hint, Func<ISmartNvme, double?> selector)
    {
        lock (disks)
        {
            var values = new List<Measurement<double>>(hint);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var disk in disks)
            {
                // TODO Last update
                if (disk.Smart is ISmartNvme smart)
                {
                    var value = selector(smart);
                    if (value.HasValue)
                    {
                        values.Add(new Measurement<double>(value.Value, MakeTags(disk.Index, disk.Model)));
                    }
                }
            }

            return values;
        }
    }
}
