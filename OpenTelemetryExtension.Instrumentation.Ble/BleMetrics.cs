namespace OpenTelemetryExtension.Instrumentation.Ble;

using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Windows.Devices.Bluetooth.Advertisement;

internal sealed class BleMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(BleMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly int signalThreshold;

    private readonly int timeThreshold;

    private readonly bool knownOnly;

    private readonly Dictionary<ulong, DeviceEntry> knownDevices;

    private readonly SortedDictionary<ulong, Device> detectedDevice = [];

    private readonly BluetoothLEAdvertisementWatcher watcher;

    public BleMetrics(
        ILogger<BleMetrics> log,
        BleOptions options)
    {
        log.InfoMetricsEnabled(nameof(BleMetrics));

        host = options.Host;
        signalThreshold = options.SignalThreshold;
        timeThreshold = options.TimeThreshold;
        knownOnly = options.KnownOnly;
        knownDevices = options.KnownDevice
            .ToDictionary(static x => Convert.ToUInt64(x.Address.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal), 16));

        MeterInstance.CreateObservableGauge("ble.rssi", Measure);

        watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };
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

    private KeyValuePair<string, object?>[] MakeTags(Device device)
    {
        var address = $"{device.Address:X12}";
        var name = device.Setting?.Name ?? $"({address})";
        return [new("host", host), new("address", address), new("name", name)];
    }

    private List<Measurement<double>> Measure()
    {
        var values = new List<Measurement<double>>();

        var now = DateTime.Now;
        lock (detectedDevice)
        {
            var todoRemove = default(List<ulong>);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var (address, device) in detectedDevice)
            {
                if ((now - device.LastUpdate).TotalMilliseconds > timeThreshold)
                {
                    todoRemove ??= [];
                    todoRemove.Add(address);
                    continue;
                }

                values.Add(new Measurement<double>(device.Rssi, MakeTags(device)));
            }

            if (todoRemove is not null)
            {
                foreach (var address in todoRemove)
                {
                    detectedDevice.Remove(address);
                }
            }
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // EVent
    //--------------------------------------------------------------------------------

    private void OnWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        if (args.RawSignalStrengthInDBm <= signalThreshold)
        {
            return;
        }

        lock (detectedDevice)
        {
            if (!detectedDevice.TryGetValue(args.BluetoothAddress, out var device))
            {
                var setting = knownDevices.Count > 0 ? knownDevices.GetValueOrDefault(args.BluetoothAddress) : null;
                if (knownOnly && (setting is null))
                {
                    return;
                }

                device = new Device(args.BluetoothAddress, setting);
                detectedDevice[args.BluetoothAddress] = device;
            }

            device.LastUpdate = DateTime.Now;
            device.Rssi = args.RawSignalStrengthInDBm;
        }
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private sealed class Device
    {
        public ulong Address { get; }

        public DateTime LastUpdate { get; set; }

        public double Rssi { get; set; }

        public DeviceEntry? Setting { get; }

        public Device(ulong address, DeviceEntry? setting)
        {
            Address = address;
            Setting = setting;
        }
    }
}
