namespace OpenTelemetryExtension.Instrumentation.SwitchBot.Windows;

using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.Extensions.Logging;

using global::Windows.Devices.Bluetooth.Advertisement;

internal sealed class SwitchBotMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(SwitchBotMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly SwitchBotOptions options;

    private readonly Device[] devices;

    private readonly BluetoothLEAdvertisementWatcher watcher;

    public SwitchBotMetrics(
        ILogger<SwitchBotMetrics> log,
        SwitchBotOptions options)
    {
        log.InfoMetricsEnabled(nameof(SwitchBotMetrics));

        this.options = options;
        devices = options.Device.Select(static x => CreateDevice(x)).ToArray();
        var meters = devices.Count(static x => x.Setting.Type == DeviceType.Meter);
        var plugs = devices.Count(static x => x.Setting.Type == DeviceType.PlugMini);

        MeterInstance.CreateObservableUpDownCounter(
            "sensor.rssi",
            () => ToMeasurement<Device>(devices.Length, static x => x.Rssi));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.temperature",
            () => ToMeasurement<MeterDevice>(meters, static x => x.Temperature));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.humidity",
            () => ToMeasurement<MeterDevice>(meters, static x => x.Humidity));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.co2",
            () => ToMeasurement<MeterDevice>(meters, static x => x.Co2));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.power",
            () => ToMeasurement<PlugMiniDevice>(plugs, static x => x.Power));

        watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };
        watcher.Received += OnWatcherReceived;
        watcher.Start();
    }

    private static Device CreateDevice(DeviceEntry entry)
    {
        return entry.Type switch
        {
            DeviceType.Meter => new MeterDevice(entry),
            DeviceType.PlugMini => new PlugMiniDevice(entry),
            _ => throw new ArgumentException($"Invalid device type. type=[{entry.Type}]")
        };
    }

    public void Dispose()
    {
        watcher.Stop();
        watcher.Received -= OnWatcherReceived;
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> ToMeasurement<T>(int hint, Func<T, double?> selector)
        where T : Device
    {
        var values = new List<Measurement<double>>(hint);

        var now = DateTime.Now;
        lock (devices)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var device in devices.OfType<T>())
            {
                if ((now - device.LastUpdate).TotalSeconds > options.TimeThreshold)
                {
                    continue;
                }

                var value = selector(device);
                if (value.HasValue)
                {
                    values.Add(new Measurement<double>(
                        value.Value,
                        new("model", "switchbot"),
                        new("address", device.Setting.Address),
                        new("name", device.Setting.Name)));
                }
            }
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        foreach (var md in args.Advertisement.ManufacturerData.Where(static x => x.CompanyId == 0x0969))
        {
            lock (devices)
            {
                var device = devices.FirstOrDefault(x => x.Address == args.BluetoothAddress);
                if (device is null)
                {
                    return;
                }

                device.LastUpdate = DateTime.Now;
                device.Rssi = args.RawSignalStrengthInDBm;

                var buffer = md.Data.ToArray();
                if (device is MeterDevice meter)
                {
                    if (buffer.Length >= 11)
                    {
                        meter.Temperature = (((double)(buffer[8] & 0x0f) / 10) + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
                        meter.Humidity = buffer[10] & 0x7f;
                        meter.Co2 = buffer.Length >= 16 ? (buffer[13] << 8) + buffer[14] : null;
                    }
                }
                else if (device is PlugMiniDevice plug)
                {
                    if (buffer.Length >= 12)
                    {
                        plug.Power = (double)(((buffer[10] & 0b00111111) << 8) + (buffer[11] & 0b01111111)) / 10;
                    }
                }
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private abstract class Device
    {
        public ulong Address { get; }

        public DeviceEntry Setting { get; }

        public DateTime LastUpdate { get; set; }

        public double Rssi { get; set; }

        protected Device(DeviceEntry setting)
        {
            Setting = setting;
            Address = Convert.ToUInt64(
                setting.Address.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal),
                16);
        }
    }

    private sealed class MeterDevice : Device
    {
        public double Temperature { get; set; }

        public double Humidity { get; set; }

        public double? Co2 { get; set; }

        public MeterDevice(DeviceEntry setting)
            : base(setting)
        {
        }
    }

    private sealed class PlugMiniDevice : Device
    {
        public double Power { get; set; }

        public PlugMiniDevice(DeviceEntry setting)
            : base(setting)
        {
        }
    }
}
