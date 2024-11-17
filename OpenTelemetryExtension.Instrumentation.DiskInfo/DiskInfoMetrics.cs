namespace OpenTelemetryExtension.Instrumentation.DiskInfo;

using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;

using HardwareInfo.Disk;

using Microsoft.Extensions.Logging;

internal sealed class DiskInfoMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(DiskInfoMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly IReadOnlyList<IDiskInfo> disks;

    private readonly DiskEntry<ISmartNvme>[] nvmeEntries;

    private readonly DiskEntry<ISmartGeneric>[] genericEntries;

    private readonly int genericHint;

    private readonly Timer timer;

    public DiskInfoMetrics(
        ILogger<DiskInfoMetrics> log,
        DiskInfoOptions options)
    {
        log.InfoMetricsEnabled(nameof(DiskInfoMetrics));

        host = options.Host;

        disks = DiskInfo.GetInformation();

        nvmeEntries = disks
            .Where(static x => x.SmartType == SmartType.Nvme)
            .Select(static x => new DiskEntry<ISmartNvme>(x, (ISmartNvme)x.Smart, MakeDriveValue(x)))
            .ToArray();
        genericEntries = disks
            .Where(static x => x.SmartType == SmartType.Generic)
            .Select(static x => new DiskEntry<ISmartGeneric>(x, (ISmartGeneric)x.Smart, MakeDriveValue(x)))
            .ToArray();
        genericHint = genericEntries.Sum(static x => x.Smart.GetSupportedIds().Count);

        MeterInstance.CreateObservableUpDownCounter("smart.disk.byte_per_sector", MeasureDisk);
        MeterInstance.CreateObservableUpDownCounter("smart.nvme.value", MeasureNvme);
        MeterInstance.CreateObservableUpDownCounter("smart.generic.value", MeasureGeneric);

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

    private static string MakeDriveValue(IDiskInfo disk) =>
        String.Concat(disk.GetDrives().Select(static x => x.Name.TrimEnd(':')));

    private KeyValuePair<string, object?>[] MakeTags(uint index, string model, string drive) =>
        [new("host", host), new("index", index), new("model", model), new("drive", drive)];

    private KeyValuePair<string, object?>[] MakeTags(uint index, string model, string drive, string id) =>
        [new("host", host), new("index", index), new("model", model), new("drive", drive), new("smart_id", id)];

    private Measurement<double> MakeMeasurement<T>(DiskEntry<T> entry, string id, double value) =>
        new(value, MakeTags(entry.Disk.Index, entry.Disk.Model, entry.Drive, id));

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> MeasureDisk()
    {
        lock (disks)
        {
            var values = new List<Measurement<double>>(nvmeEntries.Length + genericEntries.Length);

            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var entry in nvmeEntries)
            {
                values.Add(new Measurement<double>(entry.Disk.BytesPerSector, MakeTags(entry.Disk.Index, entry.Disk.Model, entry.Drive)));
            }

            foreach (var entry in genericEntries)
            {
                values.Add(new Measurement<double>(entry.Disk.BytesPerSector, MakeTags(entry.Disk.Index, entry.Disk.Model, entry.Drive)));
            }
            // ReSharper restore LoopCanBeConvertedToQuery

            return values;
        }
    }

    private List<Measurement<double>> MeasureNvme()
    {
        lock (disks)
        {
            var values = new List<Measurement<double>>(nvmeEntries.Length * 25);

            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var entry in nvmeEntries)
            {
                var smart = entry.Smart;
                if (smart.LastUpdate)
                {
                    values.Add(MakeMeasurement(entry, "available_spare", smart.AvailableSpare));
                    values.Add(MakeMeasurement(entry, "available_spare_threshold", smart.AvailableSpareThreshold));
                    values.Add(MakeMeasurement(entry, "controller_busy_time", smart.ControllerBusyTime));
                    values.Add(MakeMeasurement(entry, "critical_composite_temperature_time", smart.CriticalCompositeTemperatureTime));
                    values.Add(MakeMeasurement(entry, "critical_warning", smart.CriticalWarning));
                    values.Add(MakeMeasurement(entry, "data_unit_read", smart.DataUnitRead));
                    values.Add(MakeMeasurement(entry, "data_unit_written", smart.DataUnitWritten));
                    values.Add(MakeMeasurement(entry, "error_info_log_entries", smart.ErrorInfoLogEntries));
                    values.Add(MakeMeasurement(entry, "host_read_commands", smart.HostReadCommands));
                    values.Add(MakeMeasurement(entry, "host_write_commands", smart.HostWriteCommands));
                    values.Add(MakeMeasurement(entry, "media_errors", smart.MediaErrors));
                    values.Add(MakeMeasurement(entry, "percentage_used", smart.PercentageUsed));
                    values.Add(MakeMeasurement(entry, "power_cycles", smart.PowerCycles));
                    values.Add(MakeMeasurement(entry, "power_on_hours", smart.PowerOnHours));
                    values.Add(MakeMeasurement(entry, "temperature", smart.Temperature));
                    values.Add(MakeMeasurement(entry, "unsafe_shutdowns", smart.UnsafeShutdowns));
                    values.Add(MakeMeasurement(entry, "warning_composite_temperature_time", smart.WarningCompositeTemperatureTime));
                    for (var i = 0; i < smart.TemperatureSensors.Length; i++)
                    {
                        var value = smart.TemperatureSensors[i];
                        if (value > 0)
                        {
                            values.Add(MakeMeasurement(entry, $"temperature_sensor{i}", value));
                        }
                    }
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery

            return values;
        }
    }

    private List<Measurement<double>> MeasureGeneric()
    {
        lock (disks)
        {
            var values = new List<Measurement<double>>(genericHint);

            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var entry in genericEntries)
            {
                var smart = entry.Smart;
                if (smart.LastUpdate)
                {
                    foreach (var id in smart.GetSupportedIds())
                    {
                        var attr = smart.GetAttribute(id);
                        if (attr.HasValue)
                        {
                            values.Add(MakeMeasurement(entry, $"{(byte)id:X2}", attr.Value.RawValue));
                        }
                    }
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery

            return values;
        }
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private sealed record DiskEntry<T>(IDiskInfo Disk, T Smart, string Drive);
}
