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

    private readonly SwitchBotInstrumentationOptions options;

    private readonly BluetoothLEAdvertisementWatcher watcher;

    private readonly SortedDictionary<ulong, Data> sensorData = [];

    public SwitchBotMetrics(SwitchBotInstrumentationOptions options)
    {
        this.options = options;

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
    // Event
    //--------------------------------------------------------------------------------

    private void OnWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        foreach (var md in args.Advertisement.ManufacturerData.Where(static x => x.CompanyId == 0x0969))
        {
            var buffer = md.Data.ToArray();
            if (buffer.Length >= 11)
            {
                lock (sensorData)
                {
                    if (!sensorData.TryGetValue(args.BluetoothAddress, out var data))
                    {
                        data = new Data { Address = args.BluetoothAddress };
                        sensorData[args.BluetoothAddress] = data;
                    }

                    data.LastUpdate = DateTime.Now;
                    data.Rssi = args.RawSignalStrengthInDBm;
                    data.Temperature = (((double)(buffer[8] & 0x0f) / 10) + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
                    data.Humidity = buffer[10] & 0x7f;
                    data.Co2 = buffer.Length >= 16 ? (buffer[13] << 8) + buffer[14] : null;
                }
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> ToMeasurement(Func<Data, double?> converter)
    {
        var values = new List<Measurement<double>>();

        lock (sensorData)
        {
            var removes = default(List<ulong>?);

            var now = DateTime.Now;
            foreach (var (key, data) in sensorData)
            {
                if ((now - data.LastUpdate).TotalSeconds > options.TimeThreshold)
                {
                    removes ??= [];
                    removes.Add(key);
                }
                else
                {
                    var value = converter(data);
                    if (value.HasValue)
                    {
                        values.Add(new Measurement<double>(value.Value, new("model", "switchbot"), new("id", data.Address)));
                    }
                }
            }

            if (removes is not null)
            {
                foreach (var key in removes)
                {
                    sensorData.Remove(key);
                }
            }
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    private sealed class Data
    {
        public required ulong Address { get; init; }

        public DateTime LastUpdate { get; set; }

        public double Rssi { get; set; }

        public double Temperature { get; set; }

        public double Humidity { get; set; }

        public double? Co2 { get; set; }
    }
}
