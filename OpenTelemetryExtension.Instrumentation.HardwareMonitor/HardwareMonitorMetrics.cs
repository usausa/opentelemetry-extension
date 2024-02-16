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

        // TODO
        SetupMemoryMeasurement();
        SetupNetworkMeasurement();

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

    private List<ISensor> EnumerableSensors(HardwareType hardwareType, SensorType sensorType) =>
        computer.Hardware.SelectMany(EnumerableSensors).Where(x => x.Hardware.HardwareType == hardwareType && x.SensorType == sensorType).ToList();

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
        var memoryDataSensors = EnumerableSensors(HardwareType.Memory, SensorType.Data);
        var memoryLoadSensors = EnumerableSensors(HardwareType.Memory, SensorType.Load);

        // Memory Used
        if (memoryDataSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "memory.used",
                () => MeasureMemory(
                    memoryDataSensors.First(x => x.Name == "Memory Used"),
                    memoryDataSensors.First(x => x.Name == "Virtual Memory Used")),
                description: "Memory used.");
        }

        // Memory Available
        if (memoryDataSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "memory.available",
                () => MeasureMemory(
                    memoryDataSensors.First(x => x.Name == "Memory Available"),
                    memoryDataSensors.First(x => x.Name == "Virtual Memory Available")),
                description: "Memory available.");
        }

        // Memory load
        if (memoryLoadSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "memory.load",
                () => MeasureMemory(
                    memoryLoadSensors.First(x => x.Name == "Memory"),
                    memoryLoadSensors.First(x => x.Name == "Virtual Memory")),
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
    // Memory
    //--------------------------------------------------------------------------------

    private void SetupNetworkMeasurement()
    {
        var networkDataSensors = EnumerableSensors(HardwareType.Network, SensorType.Data);
        var networkThroughputSensors = EnumerableSensors(HardwareType.Network, SensorType.Throughput);
        var networkLoadSensors = EnumerableSensors(HardwareType.Network, SensorType.Load);

        // Network Bytes
        if (networkDataSensors.Count > 0)
        {
            MeterInstance.CreateObservableCounter(
                "network.bytes",
                () => MeasureNetwork(
                    networkDataSensors.Where(x => x.Name == "Data Uploaded").ToArray(),
                    networkDataSensors.Where(x => x.Name == "Data Downloaded").ToArray()),
                description: "Network bytes.");
        }

        // Network Speed
        if (networkDataSensors.Count > 0)
        {
            MeterInstance.CreateObservableCounter(
                "network.speed",
                () => MeasureNetwork(
                    networkThroughputSensors.Where(x => x.Name == "Upload Speed").ToArray(),
                    networkThroughputSensors.Where(x => x.Name == "Download Speed").ToArray()),
                description: "Network speed.");
        }

        // Network Load
        if (networkLoadSensors.Count > 0)
        {
            MeterInstance.CreateObservableCounter(
                "network.load",
                () => MeasureNetwork(
                    networkLoadSensors.ToArray()),
                description: "Network load.");
        }
    }

    private Measurement<double>[] MeasureNetwork(ISensor[] uploadSensors, ISensor[] downloadSensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[uploadSensors.Length + downloadSensors.Length];

            for (var i = 0; i < uploadSensors.Length; i++)
            {
                var uploadSensor = uploadSensors[i];
                var downloadSensor = downloadSensors[i];
                values[i * 2] = new Measurement<double>(ToValue(uploadSensor), new("name", uploadSensor.Hardware.Name), new("type", "upload"));
                values[(i * 2) + 1] = new Measurement<double>(ToValue(downloadSensor), new("name", downloadSensor.Hardware.Name), new("type", "download"));
            }

            return values;
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
}
