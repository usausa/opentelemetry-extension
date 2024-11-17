namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;

using LibreHardwareMonitor.Hardware;

using Microsoft.Extensions.Logging;

internal sealed class HardwareMonitorMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(HardwareMonitorMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    private readonly Computer computer;

    private readonly UpdateVisitor updateVisitor = new();

    private readonly Timer timer;

    public HardwareMonitorMetrics(
        ILogger<HardwareMonitorMetrics> log,
        HardwareMonitorOptions options)
    {
        log.InfoMetricsEnabled(nameof(HardwareMonitorMetrics));

        host = options.Host;

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

        SetupInformationMeasurement();
        SetupCpuMeasurement();
        SetupGpuMeasurement();
        SetupMemoryMeasurement();
        SetupIoMeasurement();
        SetupBatteryMeasurement();
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

    private IEnumerable<IHardware> EnumerateHardware(HardwareType type) =>
        computer.Hardware.SelectMany(EnumerateHardware).Where(x => x.HardwareType == type);

    private IEnumerable<IHardware> EnumerateHardware(params HardwareType[] types) =>
        computer.Hardware.SelectMany(EnumerateHardware).Where(x => Array.IndexOf(types, x) >= 0);

    private static IEnumerable<IHardware> EnumerateHardware(IHardware hardware)
    {
        yield return hardware;

        foreach (var subHardware in hardware.SubHardware)
        {
            foreach (var subSubHardware in EnumerateHardware(subHardware))
            {
                yield return subSubHardware;
            }
        }
    }

    private IEnumerable<ISensor> EnumerateSensors(HardwareType hardwareType, SensorType sensorType) =>
        computer.Hardware
            .SelectMany(EnumerateSensors)
            .Where(x => (x.Hardware.HardwareType == hardwareType) &&
                        (x.SensorType == sensorType));

    private IEnumerable<ISensor> EnumerateGpuSensors(SensorType sensorType) =>
        computer.Hardware
            .SelectMany(EnumerateSensors)
            .Where(x => ((x.Hardware.HardwareType == HardwareType.GpuNvidia) ||
                         (x.Hardware.HardwareType == HardwareType.GpuAmd) ||
                         (x.Hardware.HardwareType == HardwareType.GpuIntel)) &&
                        (x.SensorType == sensorType));

    private static IEnumerable<ISensor> EnumerateSensors(IHardware hardware)
    {
        foreach (var subHardware in hardware.SubHardware)
        {
            foreach (var sensor in EnumerateSensors(subHardware))
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

    private KeyValuePair<string, object?>[] MakeTags(ISensor sensor) =>
        [new("host", host), new("index", sensor.Index), new("hardware", sensor.Hardware.Name), new("name", sensor.Name)];

    private KeyValuePair<string, object?>[] MakeTags(ISensor sensor, string type) =>
        [new("host", host), new("index", sensor.Index), new("hardware", sensor.Hardware.Name), new("name", sensor.Name), new("type", type)];

    private Measurement<double>[] MeasureSensor(ISensor[] sensors)
    {
        lock (computer)
        {
            var values = new Measurement<double>[sensors.Length];

            for (var i = 0; i < sensors.Length; i++)
            {
                var sensor = sensors[i];
                values[i] = new Measurement<double>(ToValue(sensor), MakeTags(sensor));
            }

            return values;
        }
    }

    //--------------------------------------------------------------------------------
    // Information
    //--------------------------------------------------------------------------------

    private void SetupInformationMeasurement()
    {
        var list = new List<KeyValuePair<string, IHardware>>();
        list.AddRange(EnumerateHardware(HardwareType.Cpu).Select(static x => new KeyValuePair<string, IHardware>("cpu", x)));
        list.AddRange(EnumerateHardware(HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel).Select(static x => new KeyValuePair<string, IHardware>("gpu", x)));
        list.AddRange(EnumerateHardware(HardwareType.Memory).Select(static x => new KeyValuePair<string, IHardware>("memory", x)));
        list.AddRange(EnumerateHardware(HardwareType.Motherboard).Select(static x => new KeyValuePair<string, IHardware>("motherboard", x)));
        list.AddRange(EnumerateHardware(HardwareType.SuperIO).Select(static x => new KeyValuePair<string, IHardware>("io", x)));
        list.AddRange(EnumerateHardware(HardwareType.Battery).Select(static x => new KeyValuePair<string, IHardware>("battery", x)));
        list.AddRange(EnumerateHardware(HardwareType.Storage).Select(static x => new KeyValuePair<string, IHardware>("storage", x)));
        list.AddRange(EnumerateHardware(HardwareType.Network).Select(static x => new KeyValuePair<string, IHardware>("network", x)));

        MeterInstance.CreateObservableUpDownCounter(
            "hardware.information",
            () => MeasureInformation(list),
            description: "Hardware information.");
    }

    private Measurement<double>[] MeasureInformation(List<KeyValuePair<string, IHardware>> list)
    {
        var values = new Measurement<double>[list.Count];

        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            values[i] = new Measurement<double>(1, [new("host", host), new("type", entry.Key), new("identifier", entry.Value.Identifier), new("name", entry.Value.Name)]);
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // CPU
    //--------------------------------------------------------------------------------

    private void SetupCpuMeasurement()
    {
        var loadSensors = EnumerateSensors(HardwareType.Cpu, SensorType.Load)
            .Where(static x => x.Name.StartsWith("CPU Core #", StringComparison.Ordinal) || x.Name == "CPU Total")
            .ToArray();
        var clockSensors = EnumerateSensors(HardwareType.Cpu, SensorType.Clock)
            .Where(static x => !x.Name.Contains("Effective", StringComparison.Ordinal))
            .ToArray();
        var temperatureSensors = EnumerateSensors(HardwareType.Cpu, SensorType.Temperature)
            .Where(static x => !x.Name.EndsWith("Distance to TjMax", StringComparison.Ordinal) &&
                               (x.Name != "Core Max") &&
                               (x.Name != "Core Average"))
            .ToArray();
        var voltageSensors = EnumerateSensors(HardwareType.Cpu, SensorType.Voltage).ToArray();
        var currentSensors = EnumerateSensors(HardwareType.Cpu, SensorType.Current).ToArray();
        var powerSensors = EnumerateSensors(HardwareType.Cpu, SensorType.Power).ToArray();

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
        var loadSensors = EnumerateGpuSensors(SensorType.Load)
            .Where(static x => x.Name.StartsWith("GPU", StringComparison.Ordinal))
            .ToArray();
        var clockSensors = EnumerateGpuSensors(SensorType.Clock).ToArray();
        var fanSensors = EnumerateGpuSensors(SensorType.Fan).ToArray();
        var temperatureSensors = EnumerateGpuSensors(SensorType.Temperature).ToArray();
        var powerSensors = EnumerateGpuSensors(SensorType.Power).ToArray();
        var memorySensors = EnumerateGpuSensors(SensorType.SmallData)
            .Where(static x => x.Name.StartsWith("GPU Memory", StringComparison.Ordinal))
            .ToArray();
        var throughputSensors = EnumerateGpuSensors(SensorType.Throughput)
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
                new Measurement<double>(ToValue(freeMemory), MakeTags(freeMemory, "free")),
                new Measurement<double>(ToValue(usedMemory), MakeTags(usedMemory, "used")),
                new Measurement<double>(ToValue(totalMemory), MakeTags(totalMemory, "total"))
            ];
        }
    }

    private Measurement<double>[] MeasureGpu(ISensor rxThroughput, ISensor txThroughput)
    {
        lock (computer)
        {
            return
            [
                new Measurement<double>(ToValue(rxThroughput), MakeTags(rxThroughput, "rx")),
                new Measurement<double>(ToValue(txThroughput), MakeTags(txThroughput, "tx"))
            ];
        }
    }

    //--------------------------------------------------------------------------------
    // Memory
    //--------------------------------------------------------------------------------

    private void SetupMemoryMeasurement()
    {
        var dataSensors = EnumerateSensors(HardwareType.Memory, SensorType.Data).ToList();
        var loadSensors = EnumerateSensors(HardwareType.Memory, SensorType.Load).ToList();

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
                new Measurement<double>(ToValue(physicalMemory), MakeTags(physicalMemory, "physical")),
                new Measurement<double>(ToValue(virtualMemory), MakeTags(virtualMemory, "virtual"))
            ];
        }
    }

    //--------------------------------------------------------------------------------
    // I/O
    //--------------------------------------------------------------------------------

    private void SetupIoMeasurement()
    {
        var controlSensors = EnumerateSensors(HardwareType.SuperIO, SensorType.Control).ToArray();
        var fanSensors = EnumerateSensors(HardwareType.SuperIO, SensorType.Fan).ToArray();
        var temperatureSensors = EnumerateSensors(HardwareType.SuperIO, SensorType.Temperature).ToArray();
        var voltageSensors = EnumerateSensors(HardwareType.SuperIO, SensorType.Voltage).ToArray();

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
    // Battery
    //--------------------------------------------------------------------------------

    private void SetupBatteryMeasurement()
    {
        var levelChargeSensor = EnumerateSensors(HardwareType.Battery, SensorType.Voltage).FirstOrDefault(static x => x.Name == "Charge Level");
        var levelDegradationSensor = EnumerateSensors(HardwareType.Battery, SensorType.Voltage).FirstOrDefault(static x => x.Name == "Degradation Level");
        var voltageSensor = EnumerateSensors(HardwareType.Battery, SensorType.Voltage).FirstOrDefault();
        var currentSensor = EnumerateSensors(HardwareType.Battery, SensorType.Current).FirstOrDefault();
        var energySensors = EnumerateSensors(HardwareType.Battery, SensorType.Energy).ToList();
        var powerSensor = EnumerateSensors(HardwareType.Battery, SensorType.Power).FirstOrDefault();
        var timespanSensor = EnumerateSensors(HardwareType.Battery, SensorType.TimeSpan).FirstOrDefault();

        // Battery charge
        if (levelChargeSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.charge",
                () => MeasureSimpleBattery(levelChargeSensor),
                description: "Battery charge.");
        }

        // Battery degradation
        if (levelDegradationSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.degradation",
                () => MeasureSimpleBattery(levelDegradationSensor),
                description: "Battery degradation.");
        }

        // Battery voltage
        if (voltageSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.voltage",
                () => MeasureSimpleBattery(voltageSensor),
                description: "Battery voltage.");
        }

        // Battery current
        if (currentSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.current",
                () => MeasureSimpleBattery(currentSensor),
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
                () => MeasureSimpleBattery(powerSensor),
                description: "Battery rate.");
        }

        // Battery remaining
        if (timespanSensor is not null)
        {
            MeterInstance.CreateObservableUpDownCounter(
                "hardware.battery.remaining",
                () => MeasureSimpleBattery(timespanSensor),
                description: "Battery remaining.");
        }
    }

    private Measurement<double> MeasureSimpleBattery(ISensor sensor)
    {
        lock (computer)
        {
            return new Measurement<double>(ToValue(sensor), MakeTags(sensor));
        }
    }

    private Measurement<double>[] MeasureBatteryCapacity(ISensor designed, ISensor fullCharged, ISensor remaining)
    {
        lock (computer)
        {
            return
            [
                new Measurement<double>(ToValue(designed), MakeTags(designed, "designed")),
                new Measurement<double>(ToValue(fullCharged), MakeTags(fullCharged, "full")),
                new Measurement<double>(ToValue(remaining), MakeTags(remaining, "remaining"))
            ];
        }
    }

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    private void SetupStorageMeasurement()
    {
        // TODO PowerOn hours, Device power cycle count (LibreHardwareMonitorLib not supported)

        var loadSensors = EnumerateSensors(HardwareType.Storage, SensorType.Load).ToList();
        var dataSensors = EnumerateSensors(HardwareType.Storage, SensorType.Data).ToList();
        var throughputSensors = EnumerateSensors(HardwareType.Storage, SensorType.Throughput).ToList();
        var temperatureSensors = EnumerateSensors(HardwareType.Storage, SensorType.Temperature).ToArray();
        var levelSensors = EnumerateSensors(HardwareType.Storage, SensorType.Level).ToList();
        var factorSensors = EnumerateSensors(HardwareType.Storage, SensorType.Factor).ToList();

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
                values[i] = new Measurement<double>(ToValue(sensor), MakeTags(sensor));
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
                values[i * 2] = new Measurement<double>(ToValue(readSensor), MakeTags(readSensor, "read"));
                values[(i * 2) + 1] = new Measurement<double>(ToValue(writeSensor), MakeTags(writeSensor, "write"));
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
                    values[i] = new Measurement<double>(100 - ToValue(sensor), MakeTags(sensor));
                }
                else
                {
                    values[i] = new Measurement<double>(ToValue(sensor), MakeTags(sensor));
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
        var dataSensors = EnumerateSensors(HardwareType.Network, SensorType.Data).ToList();
        var throughputSensors = EnumerateSensors(HardwareType.Network, SensorType.Throughput).ToList();
        var loadSensors = EnumerateSensors(HardwareType.Network, SensorType.Load).ToArray();

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
                values[i] = new Measurement<double>(ToValue(sensor), MakeTags(sensor));
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
                values[i * 2] = new Measurement<double>(ToValue(downloadSensor), MakeTags(downloadSensor, "download"));
                values[(i * 2) + 1] = new Measurement<double>(ToValue(uploadSensor), MakeTags(uploadSensor, "upload"));
            }

            return values;
        }
    }
}
