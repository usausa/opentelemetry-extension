namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

using System.Diagnostics.Metrics;
using System.Reflection;

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
        computer.Accept(updateVisitor);

        // TODO
        MeterInstance.CreateObservableUpDownCounter(
            "test",
            () =>
            {
                return new[]
                {
                    new Measurement<double>(0, new("instance", "0"), new("type", "x"))
                };
            },
            unit: "X",
            description: "test");

        timer = new Timer(Update, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    public void Dispose()
    {
        timer.Dispose();
    }

    private void Update(object? state)
    {
        computer.Accept(updateVisitor);
    }
}
