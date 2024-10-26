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

        SetupBatteryMeasurement();
        SetupCpuMeasurement();
        SetupGpuMeasurement();
        SetupIoMeasurement();
        SetupMemoryMeasurement();
        SetupStorageMeasurement();
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

    private IEnumerable<ISensor> EnumerableSensors(HardwareType hardwareType, SensorType sensorType) =>
        computer.Hardware
            .SelectMany(EnumerableSensors)
            .Where(x => (x.Hardware.HardwareType == hardwareType) &&
                        (x.SensorType == sensorType));

    private IEnumerable<ISensor> EnumerableGpuSensors(SensorType sensorType) =>
        computer.Hardware
            .SelectMany(EnumerableSensors)
            .Where(x => ((x.Hardware.HardwareType == HardwareType.GpuNvidia) ||
                         (x.Hardware.HardwareType == HardwareType.GpuAmd) ||
                         (x.Hardware.HardwareType == HardwareType.GpuIntel)) &&
                        (x.SensorType == sensorType));

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
    // Shared
    //--------------------------------------------------------------------------------

    private double MeasureSimple(ISensor sensor)
    {
        lock (computer)
        {
            return ToValue(sensor);
        }
    }

    private Measurement<double>[] MeasureSensor(ISensor[] sensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[sensors.Length];

            for (var i = 0; i < sensors.Length; i++)
            {
                var sensor = sensors[i];
                values[i] = new Measurement<double>(ToValue(sensor), new KeyValuePair<string, object?>[] { new("name", sensor.Name) });
            }

            return values;
        }
    }

    //--------------------------------------------------------------------------------
    // Battery
    //--------------------------------------------------------------------------------

    private void SetupBatteryMeasurement()
    {
        var levelChargeSensor = EnumerableSensors(HardwareType.Battery, SensorType.Voltage).FirstOrDefault(static x => x.Name == "Charge Level");
        var levelDegradationSensor = EnumerableSensors(HardwareType.Battery, SensorType.Voltage).FirstOrDefault(static x => x.Name == "Degradation Level");
        var voltageSensor = EnumerableSensors(HardwareType.Battery, SensorType.Voltage).FirstOrDefault();
        var currentSensor = EnumerableSensors(HardwareType.Battery, SensorType.Current).FirstOrDefault();
        var energySensors = EnumerableSensors(HardwareType.Battery, SensorType.Energy).ToList();
        var powerSensor = EnumerableSensors(HardwareType.Battery, SensorType.Power).FirstOrDefault();
        var timespanSensor = EnumerableSensors(HardwareType.Battery, SensorType.TimeSpan).FirstOrDefault();

        // Battery charge
        if (levelChargeSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.charge",
                () => MeasureSimple(levelChargeSensor),
                description: "Battery charge.");
        }

        // Battery degradation
        if (levelDegradationSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.degradation",
                () => MeasureSimple(levelDegradationSensor),
                description: "Battery degradation.");
        }

        // Battery voltage
        if (voltageSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.voltage",
                () => MeasureSimple(voltageSensor),
                description: "Battery voltage.");
        }

        // Battery current
        if (currentSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.current",
                () => MeasureSimple(currentSensor),
                description: "Battery current.");
        }

        // Battery capacity
        if (energySensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.capacity",
                () => MeasureBatteryCapacity(
                    energySensors.First(static x => x.Name == "Designed Capacity"),
                    energySensors.First(static x => x.Name == "Full Charged Capacity"),
                    energySensors.First(static x => x.Name == "Remaining Capacity")),
                description: "Battery capacity.");
        }

        // Battery rate
        if (powerSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.rate",
                () => MeasureSimple(powerSensor),
                description: "Battery rate.");
        }

        // Battery remaining
        if (timespanSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.remaining",
                () => MeasureSimple(timespanSensor),
                description: "Battery remaining.");
        }
    }

    private Measurement<double>[] MeasureBatteryCapacity(ISensor designed, ISensor fullCharged, ISensor remaining)
    {
        lock (computer)
        {
            return
            [
                new Measurement<double>(ToValue(designed), new KeyValuePair<string, object?>[] { new("type", "designed") }),
                new Measurement<double>(ToValue(fullCharged), new KeyValuePair<string, object?>[] { new("type", "full") }),
                new Measurement<double>(ToValue(remaining), new KeyValuePair<string, object?>[] { new("type", "remaining") })
            ];
        }
    }

    //--------------------------------------------------------------------------------
    // CPU
    //--------------------------------------------------------------------------------

    private void SetupCpuMeasurement()
    {
        var loadSensors = EnumerableSensors(HardwareType.Cpu, SensorType.Load)
            .Where(static x => x.Name.StartsWith("CPU Core #", StringComparison.Ordinal))
            .ToArray();
        var clockSensors = EnumerableSensors(HardwareType.Cpu, SensorType.Clock)
            .Where(static x => !x.Name.Contains("Effective", StringComparison.Ordinal))
            .ToArray();
        var temperatureSensors = EnumerableSensors(HardwareType.Cpu, SensorType.Temperature)
            .Where(static x => !x.Name.EndsWith("Distance to TjMax", StringComparison.Ordinal) &&
                               (x.Name != "Core Max") &&
                               (x.Name != "Core Average"))
            .ToArray();
        var voltageSensors = EnumerableSensors(HardwareType.Cpu, SensorType.Voltage).ToArray();
        var currentSensors = EnumerableSensors(HardwareType.Cpu, SensorType.Current).ToArray();
        var powerSensors = EnumerableSensors(HardwareType.Cpu, SensorType.Power).ToArray();

        // CPU load
        if (loadSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.cpu.load",
                () => MeasureSensor(loadSensors),
                description: "CPU load.");
        }

        // CPU clock
        if (clockSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.cpu.clock",
                () => MeasureSensor(clockSensors),
                description: "CPU clock.");
        }

        // CPU temperature
        if (temperatureSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.cpu.temperature",
                () => MeasureSensor(temperatureSensors),
                description: "CPU temperature.");
        }

        // CPU voltage
        if (voltageSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.cpu.voltage",
                () => MeasureSensor(voltageSensors),
                description: "CPU voltage.");
        }

        // CPU current
        if (currentSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.cpu.current",
                () => MeasureSensor(currentSensors),
                description: "CPU current.");
        }

        // CPU power
        if (powerSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.cpu.power",
                () => MeasureSensor(powerSensors),
                description: "CPU power.");
        }
    }

    //--------------------------------------------------------------------------------
    // GPU
    //--------------------------------------------------------------------------------

    private void SetupGpuMeasurement()
    {
        var loadSensors = EnumerableGpuSensors(SensorType.Load)
            .Where(static x => x.Name.StartsWith("GPU", StringComparison.Ordinal))
            .ToArray();
        var clockSensors = EnumerableGpuSensors(SensorType.Clock).ToArray();
        var fanSensors = EnumerableGpuSensors(SensorType.Fan).ToArray();
        var temperatureSensors = EnumerableGpuSensors(SensorType.Temperature).ToArray();
        var powerSensors = EnumerableGpuSensors(SensorType.Power).ToArray();
        var memorySensors = EnumerableGpuSensors(SensorType.SmallData)
            .Where(static x => x.Name.StartsWith("GPU Memory", StringComparison.Ordinal))
            .ToArray();
        var throughputSensors = EnumerableGpuSensors(SensorType.Throughput)
            .Where(static x => x.Name.StartsWith("GPU PCIe", StringComparison.Ordinal))
            .ToArray();

        // GPU load
        if (loadSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.load",
                () => MeasureSensor(loadSensors),
                description: "GPU load.");
        }

        // GPU clock
        if (clockSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.clock",
                () => MeasureSensor(clockSensors),
                description: "GPU clock.");
        }

        // GPU fan
        if (fanSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.fan",
                () => MeasureSensor(fanSensors),
                description: "GPU fan.");
        }

        // GPU temperature
        if (temperatureSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.temperature",
                () => MeasureSensor(temperatureSensors),
                description: "GPU temperature.");
        }

        // GPU power
        if (powerSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.power",
                () => MeasureSensor(powerSensors),
                description: "GPU power.");
        }

        // GPU memory
        if (memorySensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.memory",
                () => MeasureGpu(
                    memorySensors.First(static x => x.Name == "GPU Memory Free"),
                    memorySensors.First(static x => x.Name == "GPU Memory Used"),
                    memorySensors.First(static x => x.Name == "GPU Memory Total")),
                description: "GPU memory.");
        }

        // GPU throughput
        if (throughputSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.gpu.throughput",
                () => MeasureGpu(
                    throughputSensors.First(static x => x.Name == "GPU PCIe Rx"),
                    throughputSensors.First(static x => x.Name == "GPU PCIe Tx")),
                description: "GPU throughput.");
        }
    }

    private Measurement<double>[] MeasureGpu(ISensor freeMemory, ISensor usedMemory, ISensor totalMemory)
    {
        lock (computer)
        {
            return
            [
                new Measurement<double>(ToValue(freeMemory), new KeyValuePair<string, object?>[] { new("type", "free") }),
                new Measurement<double>(ToValue(usedMemory), new KeyValuePair<string, object?>[] { new("type", "used") }),
                new Measurement<double>(ToValue(totalMemory), new KeyValuePair<string, object?>[] { new("type", "total") })
            ];
        }
    }

    private Measurement<double>[] MeasureGpu(ISensor rxThroughput, ISensor txThroughput)
    {
        lock (computer)
        {
            return
            [
                new Measurement<double>(ToValue(rxThroughput), new KeyValuePair<string, object?>[] { new("type", "rx") }),
                new Measurement<double>(ToValue(txThroughput), new KeyValuePair<string, object?>[] { new("type", "tx") })
            ];
        }
    }

    //--------------------------------------------------------------------------------
    // I/O
    //--------------------------------------------------------------------------------

    private void SetupIoMeasurement()
    {
        var controlSensors = EnumerableSensors(HardwareType.SuperIO, SensorType.Control).ToArray();
        var fanSensors = EnumerableSensors(HardwareType.SuperIO, SensorType.Fan).ToArray();
        var temperatureSensors = EnumerableSensors(HardwareType.SuperIO, SensorType.Temperature).ToArray();
        var voltageSensors = EnumerableSensors(HardwareType.SuperIO, SensorType.Voltage).ToArray();

        // I/O control
        if (controlSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.io.control",
                () => MeasureSensor(controlSensors),
                description: "I/O control.");
        }

        // I/O fan
        if (fanSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.io.fan",
                () => MeasureSensor(fanSensors),
                description: "I/O fan.");
        }

        // I/O temperature
        if (temperatureSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.io.temperature",
                () => MeasureSensor(temperatureSensors),
                description: "I/O temperature.");
        }

        // I/O voltage
        if (voltageSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.io.voltage",
                () => MeasureSensor(voltageSensors),
                description: "I/O voltage.");
        }
    }

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
                "hardware.memory.used",
                () => MeasureMemory(
                    dataSensors.First(static x => x.Name == "Memory Used"),
                    dataSensors.First(static x => x.Name == "Virtual Memory Used")),
                description: "Memory used.");
        }

        // Memory available
        if (dataSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.memory.available",
                () => MeasureMemory(
                    dataSensors.First(static x => x.Name == "Memory Available"),
                    dataSensors.First(static x => x.Name == "Virtual Memory Available")),
                description: "Memory available.");
        }

        // Memory load
        if (loadSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.memory.load",
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
                "hardware.storage.used",
                () => MeasureStorage(loadUsedSensors),
                description: "Storage used.");
        }

        // Storage bytes
        var dataReadSensors = dataSensors.Where(static x => x.Name == "Data Read").ToArray();
        var dataWriteSensors = dataSensors.Where(static x => x.Name == "Data Written").ToArray();
        if ((dataReadSensors.Length > 0) || (dataWriteSensors.Length > 0))
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.storage.bytes",
                () => MeasureStorage(dataReadSensors, dataWriteSensors),
                description: "Storage bytes.");
        }

        // Storage speed
        if (throughputSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.storage.speed",
                () => MeasureStorage(
                    throughputSensors.Where(static x => x.Name == "Read Rate").ToArray(),
                    throughputSensors.Where(static x => x.Name == "Write Rate").ToArray()),
                description: "Storage speed.");
        }

        // Storage temperature
        if (temperatureSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.storage.temperature",
                () => MeasureStorage(temperatureSensors),
                description: "Storage temperature.");
        }

        // Storage life
        var levelLifeSensors = levelSensors.Where(static x => (x.Name == "Percentage Used") || (x.Name == "Remaining Life")).ToArray();
        if (levelLifeSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.storage.life",
                () => MeasureStorageLife(levelLifeSensors),
                description: "Storage life.");
        }

        // Storage amplification
        var factorAmplificationSensors = factorSensors.Where(static x => x.Name == "Write Amplification").ToArray();
        if (factorAmplificationSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.storage.amplification",
                () => MeasureStorageLife(factorAmplificationSensors),
                description: "Storage amplification.");
        }

        // Storage spare
        var levelSpareSensors = levelSensors.Where(static x => x.Name == "Available Spare").ToArray();
        if (levelSpareSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.storage.spare",
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
                "hardware.network.bytes",
                () => MeasureNetwork(
                    dataSensors.Where(static x => x.Name == "Data Downloaded").ToArray(),
                    dataSensors.Where(static x => x.Name == "Data Uploaded").ToArray()),
                description: "Network bytes.");
        }

        // Network speed
        if (throughputSensors.Count > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.network.speed",
                () => MeasureNetwork(
                    throughputSensors.Where(static x => x.Name == "Download Speed").ToArray(),
                    throughputSensors.Where(static x => x.Name == "Upload Speed").ToArray()),
                description: "Network speed.");
        }

        // Network load
        if (loadSensors.Length > 0)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.network.load",
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
}
