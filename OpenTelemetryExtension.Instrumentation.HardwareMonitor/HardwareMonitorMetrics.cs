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
            IsPsuEnabled = options.IsPsuEnabled,
            IsStorageEnabled = options.IsStorageEnabled
        };
        computer.Open();
        computer.Accept(updateVisitor);

        // TODO
        SetupMemoryMeasurement();

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
                unit: "{GB}",
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
}
