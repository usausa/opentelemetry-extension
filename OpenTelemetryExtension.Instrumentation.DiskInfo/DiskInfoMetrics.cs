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
        MeterInstance.CreateObservableUpDownCounter("smart.availablespare",
            () => GatherMeasurementNvme(nvme, static x => x.AvailableSpare));
        MeterInstance.CreateObservableUpDownCounter("smart.availablesparethreshold",
            () => GatherMeasurementNvme(nvme, static x => x.AvailableSpareThreshold));
        MeterInstance.CreateObservableUpDownCounter("smart.controllerbusytime",
            () => GatherMeasurementNvme(nvme, static x => x.ControllerBusyTime));
        MeterInstance.CreateObservableUpDownCounter("smart.criticalcompositetemperaturetime",
            () => GatherMeasurementNvme(nvme, static x => x.CriticalCompositeTemperatureTime));
        MeterInstance.CreateObservableUpDownCounter("smart.criticalwarning",
            () => GatherMeasurementNvme(nvme, static x => x.CriticalWarning));
        MeterInstance.CreateObservableUpDownCounter("smart.dataread",
            () => GatherMeasurementNvme(nvme, static x => x.DataUnitRead * 512 * 1000));
        MeterInstance.CreateObservableUpDownCounter("smart.datawritten",
            () => GatherMeasurementNvme(nvme, static x => x.DataUnitWritten * 512 * 1000));
        MeterInstance.CreateObservableUpDownCounter("smart.errorinfologentrycount",
            () => GatherMeasurementNvme(nvme, static x => x.ErrorInfoLogEntryCount));
        MeterInstance.CreateObservableUpDownCounter("smart.hostreadcommands",
            () => GatherMeasurementNvme(nvme, static x => x.HostReadCommands));
        MeterInstance.CreateObservableUpDownCounter("smart.hostwritecommands",
            () => GatherMeasurementNvme(nvme, static x => x.HostWriteCommands));
        MeterInstance.CreateObservableUpDownCounter("smart.mediaerrors",
            () => GatherMeasurementNvme(nvme, static x => x.MediaErrors));
        MeterInstance.CreateObservableUpDownCounter("smart.percentageused",
            () => GatherMeasurementNvme(nvme, static x => x.PercentageUsed));
        MeterInstance.CreateObservableUpDownCounter("smart.powercycle",
            () => GatherMeasurementNvme(nvme, static x => x.PowerCycle));
        MeterInstance.CreateObservableUpDownCounter("smart.poweronhours",
            () => GatherMeasurementNvme(nvme, static x => x.PowerOnHours));
        MeterInstance.CreateObservableUpDownCounter("smart.temperature",
            () => GatherMeasurementNvme(nvme, static x => x.Temperature));
        MeterInstance.CreateObservableUpDownCounter("smart.unsafeshutdowns",
            () => GatherMeasurementNvme(nvme, static x => x.UnsafeShutdowns));
        MeterInstance.CreateObservableUpDownCounter("smart.warningcompositetemperaturetime",
            () => GatherMeasurementNvme(nvme, static x => x.WarningCompositeTemperatureTime));
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
                if ((disk.Smart is ISmartNvme smart) && smart.LastUpdate)
                {
                    var value = selector(smart);
                    if (value.HasValue)
                    {
                        values.Add(new Measurement<double>(value.Value, MakeTags(disk.Index, disk.Model)));
                    }
                }
                // TODO
            }

            return values;
        }
    }
}
