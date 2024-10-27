namespace OpenTelemetryExtension.Instrumentation.SwitchBot.Windows;

using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;

using global::Windows.Devices.Bluetooth.Advertisement;

internal sealed class SwitchBotMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(SwitchBotMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly SwitchBotOptions options;

    private readonly Device[] devices;

    private readonly BluetoothLEAdvertisementWatcher watcher;

    public SwitchBotMetrics(SwitchBotOptions options)
    {
        this.options = options;
        devices = options.Device.Select(static x => new Device(x)).ToArray();

        MeterInstance.CreateObservableUpDownCounter("sensor.rssi", () => ToMeasurement(static x => x.Rssi));
        MeterInstance.CreateObservableUpDownCounter("sensor.temperature", () => ToMeasurement(static x => x.Temperature));
        MeterInstance.CreateObservableUpDownCounter("sensor.humidity", () => ToMeasurement(static x => x.Humidity));
        MeterInstance.CreateObservableUpDownCounter("sensor.co2", () => ToMeasurement(static x => x.Co2));

        watcher = new BluetoothLEAdvertisementWatcher();
        watcher.Received += OnWatcherReceived;
        watcher.Start();
    }

    public void Dispose()
    {
        watcher.Stop();
        watcher.Received -= OnWatcherReceived;
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> ToMeasurement(Func<Device, double?> selector)
    {
        lock (devices)
        {
            var values = new List<Measurement<double>>(devices.Length);

            var now = DateTime.Now;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var device in devices)
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

            return values;
        }
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

                var buffer = md.Data.ToArray();
                if (buffer.Length >= 11)
                {
                    device.LastUpdate = DateTime.Now;
                    device.Rssi = args.RawSignalStrengthInDBm;
                    device.Temperature = (((double)(buffer[8] & 0x0f) / 10) + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
                    device.Humidity = buffer[10] & 0x7f;
                    device.Co2 = buffer.Length >= 16 ? (buffer[13] << 8) + buffer[14] : null;
                }
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private sealed class Device
    {
        public ulong Address { get; }

        public DeviceEntry Setting { get; }

        public DateTime LastUpdate { get; set; }

        public double Rssi { get; set; }

        public double Temperature { get; set; }

        public double Humidity { get; set; }

        public double? Co2 { get; set; }

        public Device(DeviceEntry setting)
        {
            Setting = setting;
            Address = Convert.ToUInt64(
                setting.Address.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal),
                16);
        }
    }
}
