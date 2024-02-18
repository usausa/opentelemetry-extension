namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;

using LibreHardwareMonitor.Hardware;

internal sealed class HardwareMonitorMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(HardwareMonitorMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly Computer computer;

    private readonly UpdateVisitor updateVisitor = new();

    private readonly Timer timer;

    public HardwareMonitorMetrics(HardwareMonitorInstrumentationOptions options)
    {
        computer = new Computer
        {
            IsBatteryEnabled = options.IsBatteryEnabled,
            IsControllerEnabled = options.IsControllerEnabled,
            IsCpuEnabled = options.IsCpuEnabled,
            IsGpuEnabled = options.IsGpuEnabled,
            IsMemoryEnabled = options.IsMemoryEnabled,
            IsMotherboardEnabled = options.IsMotherboardEnabled,
            IsNetworkEnabled = options.IsNetworkEnabled,
            IsStorageEnabled = options.IsStorageEnabled
        };
        computer.Open();
        computer.Accept(updateVisitor);

        // TODO Battery
        // TODO I/O
        // TODO CPU
        // TODO GPU
        SetupMemoryMeasurement();
        SetupNetworkMeasurement();
        SetupStorageMeasurement();

        timer = new Timer(Update, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    public void Dispose()
    {
        timer.Dispose();
        computer.Close();
    }

    private void Update(object? state)
    {
        lock (computer)
        {
            computer.Accept(updateVisitor);
        }
    }

    private IEnumerable<ISensor> EnumerableSensors(HardwareType hardwareType, SensorType sensorType) =>
        computer.Hardware.SelectMany(EnumerableSensors).Where(x => x.Hardware.HardwareType == hardwareType && x.SensorType == sensorType);

    private static IEnumerable<ISensor> EnumerableSensors(IHardware hardware)
    {
        foreach (var subHardware in hardware.SubHardware)
        {
            foreach (var sensor in EnumerableSensors(subHardware))
            {
                yield return sensor;
            }
        }

        foreach (var sensor in hardware.Sensors)
        {
            yield return sensor;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ToValue(ISensor sensor) => sensor.Value ?? 0;

    //--------------------------------------------------------------------------------
    // Memory
    //--------------------------------------------------------------------------------

    private void SetupMemoryMeasurement()
    {
        var dataSensors = EnumerableSensors(HardwareType.Memory, SensorType.Data).ToList();
        var loadSensors = EnumerableSensors(HardwareType.Memory, SensorType.Load).ToList();

        // Memory used
        if (dataSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "memory.used",
                () => MeasureMemory(
                    dataSensors.First(static x => x.Name == "Memory Used"),
                    dataSensors.First(static x => x.Name == "Virtual Memory Used")),
                description: "Memory used.");
        }

        // Memory available
        if (dataSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "memory.available",
                () => MeasureMemory(
                    dataSensors.First(static x => x.Name == "Memory Available"),
                    dataSensors.First(static x => x.Name == "Virtual Memory Available")),
                description: "Memory available.");
        }

        // Memory load
        if (loadSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "memory.load",
                () => MeasureMemory(
                    loadSensors.First(static x => x.Name == "Memory"),
                    loadSensors.First(static x => x.Name == "Virtual Memory")),
                description: "Memory load.");
        }
    }

    private Measurement<double>[] MeasureMemory(ISensor physicalMemory, ISensor virtualMemory)
    {
        lock (computer)
        {
            return
            [
                new Measurement<double>(ToValue(physicalMemory), new KeyValuePair<string, object?>[] { new("type", "physical") }),
                new Measurement<double>(ToValue(virtualMemory), new KeyValuePair<string, object?>[] { new("type", "virtual") })
            ];
        }
    }

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    private void SetupNetworkMeasurement()
    {
        var dataSensors = EnumerableSensors(HardwareType.Network, SensorType.Data).ToList();
        var throughputSensors = EnumerableSensors(HardwareType.Network, SensorType.Throughput).ToList();
        var loadSensors = EnumerableSensors(HardwareType.Network, SensorType.Load).ToArray();

        // Network bytes
        if (dataSensors.Count > 0)
        {
            MeterInstance.CreateObservableCounter(
                "network.bytes",
                () => MeasureNetwork(
                    dataSensors.Where(static x => x.Name == "Data Downloaded").ToArray(),
                    dataSensors.Where(static x => x.Name == "Data Uploaded").ToArray()),
                description: "Network bytes.");
        }

        // Network speed
        if (throughputSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "network.speed",
                () => MeasureNetwork(
                    throughputSensors.Where(static x => x.Name == "Download Speed").ToArray(),
                    throughputSensors.Where(static x => x.Name == "Upload Speed").ToArray()),
                description: "Network speed.");
        }

        // Network load
        if (loadSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "network.load",
                () => MeasureNetwork(loadSensors),
                description: "Network load.");
        }
    }

    private Measurement<double>[] MeasureNetwork(ISensor[] sensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[sensors.Length];

            for (var i = 0; i < sensors.Length; i++)
            {
                var sensor = sensors[i];
                values[i] = new Measurement<double>(ToValue(sensor), new KeyValuePair<string, object?>[] { new("name", sensor.Hardware.Name) });
            }

            return values;
        }
    }

    private Measurement<double>[] MeasureNetwork(ISensor[] downloadSensors, ISensor[] uploadSensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[uploadSensors.Length + downloadSensors.Length];

            for (var i = 0; i < uploadSensors.Length; i++)
            {
                var downloadSensor = downloadSensors[i];
                var uploadSensor = uploadSensors[i];
                values[i * 2] = new Measurement<double>(ToValue(downloadSensor), new("name", downloadSensor.Hardware.Name), new("type", "download"));
                values[(i * 2) + 1] = new Measurement<double>(ToValue(uploadSensor), new("name", uploadSensor.Hardware.Name), new("type", "upload"));
            }

            return values;
        }
    }

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    private void SetupStorageMeasurement()
    {
        // TODO PowerOn hours, Device power cycle count (LibreHardwareMonitorLib not supported)

        var loadSensors = EnumerableSensors(HardwareType.Storage, SensorType.Load).ToList();
        var dataSensors = EnumerableSensors(HardwareType.Storage, SensorType.Data).ToList();
        var throughputSensors = EnumerableSensors(HardwareType.Storage, SensorType.Throughput).ToList();
        var temperatureSensors = EnumerableSensors(HardwareType.Storage, SensorType.Temperature).ToArray();
        var levelSensors = EnumerableSensors(HardwareType.Storage, SensorType.Level).ToList();
        var factorSensors = EnumerableSensors(HardwareType.Storage, SensorType.Factor).ToList();

        // Storage used
        var loadUsedSensors = loadSensors.Where(static x => x.Name == "Used Space").ToArray();
        if (loadUsedSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.used",
                () => MeasureStorage(loadUsedSensors),
                description: "Storage used.");
        }

        // Storage bytes
        var dataReadSensors = dataSensors.Where(static x => x.Name == "Data Read").ToArray();
        var dataWriteSensors = dataSensors.Where(static x => x.Name == "Data Written").ToArray();
        if ((dataReadSensors.Length > 0) || (dataWriteSensors.Length > 0))
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.bytes",
                () => MeasureStorage(dataReadSensors, dataWriteSensors),
                description: "Storage bytes.");
        }

        // Storage speed
        if (throughputSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.speed",
                () => MeasureStorage(
                    throughputSensors.Where(static x => x.Name == "Read Rate").ToArray(),
                    throughputSensors.Where(static x => x.Name == "Write Rate").ToArray()),
                description: "Storage speed.");
        }

        // Storage temperature
        if (temperatureSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.temperature",
                () => MeasureStorage(temperatureSensors),
                description: "Storage temperature.");
        }

        // Storage life
        var levelLifeSensors = levelSensors.Where(static x => (x.Name == "Percentage Used") || (x.Name == "Remaining Life")).ToArray();
        if (levelLifeSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.life",
                () => MeasureStorageLife(levelLifeSensors),
                description: "Storage life.");
        }

        // Storage amplification
        var factorAmplificationSensors = factorSensors.Where(static x => x.Name == "Write Amplification").ToArray();
        if (factorAmplificationSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.amplification",
                () => MeasureStorageLife(factorAmplificationSensors),
                description: "Storage amplification.");
        }

        // Storage spare
        var levelSpareSensors = levelSensors.Where(static x => x.Name == "Available Spare").ToArray();
        if (levelSpareSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "storage.spare",
                () => MeasureStorageLife(levelSpareSensors),
                description: "Storage spare.");
        }
    }

    private Measurement<double>[] MeasureStorage(ISensor[] sensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[sensors.Length];

            for (var i = 0; i < sensors.Length; i++)
            {
                var sensor = sensors[i];
                values[i] = new Measurement<double>(ToValue(sensor), new KeyValuePair<string, object?>[] { new("name", sensor.Hardware.Name) });
            }

            return values;
        }
    }

    private Measurement<double>[] MeasureStorage(ISensor[] readSensors, ISensor[] writeSensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[writeSensors.Length + readSensors.Length];

            for (var i = 0; i < writeSensors.Length; i++)
            {
                var readSensor = readSensors[i];
                var writeSensor = writeSensors[i];
                values[i * 2] = new Measurement<double>(ToValue(readSensor), new("name", readSensor.Hardware.Name), new("type", "read"));
                values[(i * 2) + 1] = new Measurement<double>(ToValue(writeSensor), new("name", writeSensor.Hardware.Name), new("type", "write"));
            }

            return values;
        }
    }

    private Measurement<double>[] MeasureStorageLife(ISensor[] sensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[sensors.Length];

            for (var i = 0; i < sensors.Length; i++)
            {
                var sensor = sensors[i];
                if (sensor.Name == "Percentage Used")
                {
                    values[i] = new Measurement<double>(100 - ToValue(sensor), new KeyValuePair<string, object?>[] { new("name", sensor.Hardware.Name) });
                }
                else
                {
                    values[i] = new Measurement<double>(ToValue(sensor), new KeyValuePair<string, object?>[] { new("name", sensor.Hardware.Name) });
                }
            }

            return values;
        }
    }
}
