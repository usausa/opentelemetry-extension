namespace OpenTelemetryExtension.Instrumentation.HyperV;

using System.Diagnostics.Metrics;
using System.Management;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

internal sealed class HyperVMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(HyperVMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly int cacheDuration;

    private readonly Regex? filter;

    private readonly string host;

    private readonly object sync = new();

    private readonly List<VirtualMachine> virtualMachines = [];

    private DateTime nextUpdate = DateTime.MinValue;

    public HyperVMetrics(
        ILogger<HyperVMetrics> log,
        HyperVOptions options)
    {
        log.InfoMetricsEnabled(nameof(HyperVMetrics));

        cacheDuration = options.CacheDuration;
        filter = !String.IsNullOrEmpty(options.IgnoreExpression)
            ? new Regex(options.IgnoreExpression, RegexOptions.Compiled)
            : null;
        host = options.Host;

        // ReSharper disable StringLiteralTypo
        MeterInstance.CreateObservableGauge("hyperv.vm.count", MeasureCount);
        MeterInstance.CreateObservableGauge("hyperv.vm.information", MeasureInformation);
        MeterInstance.CreateObservableGauge("hyperv.vm.state", () => Measure<int>(static x => x.State));
        MeterInstance.CreateObservableGauge("hyperv.vm.processor_load", () => Measure(static x => x.ProcessorLoad));
        MeterInstance.CreateObservableGauge("hyperv.vm.memory_usage", () => Measure(static x => x.MemoryUsage));
        MeterInstance.CreateObservableGauge("hyperv.vm.uptime", () => Measure(static x => x.Uptime));
        // ReSharper restore StringLiteralTypo
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private void UpdateVirtualMachines()
    {
        if (DateTime.Now < nextUpdate)
        {
            return;
        }

        foreach (var vm in virtualMachines)
        {
            vm.Detected = false;
        }

        var added = default(bool);
        // ReSharper disable StringLiteralTypo
        using var searcher = new ManagementObjectSearcher(@"root\virtualization\v2", "SELECT * FROM Msvm_SummaryInformation");
        // ReSharper restore StringLiteralTypo
        foreach (var mo in searcher.Get())
        {
            var guid = (string)mo["Name"];
            var name = (string)mo["ElementName"];

            if (filter?.IsMatch(name) ?? false)
            {
                continue;
            }

            var vm = default(VirtualMachine);
            foreach (var virtualMachine in virtualMachines)
            {
                if (virtualMachine.Guid == guid)
                {
                    vm = virtualMachine;
                    break;
                }
            }

            if (vm is null)
            {
                vm = new VirtualMachine(guid);
                virtualMachines.Add(vm);
                added = true;
            }

            vm.Detected = true;
            vm.Name = name;
            vm.Version = (string)mo["Version"];
            vm.State = (ushort)mo["EnabledState"];
            if (vm.State is 2)
            {
                vm.ProcessorLoad = (ushort?)mo["ProcessorLoad"];
                vm.MemoryUsage = (ulong?)mo["MemoryUsage"];
                vm.Uptime = (ulong?)mo["UpTime"];
            }
            else
            {
                vm.ProcessorLoad = null;
                vm.MemoryUsage = null;
                vm.Uptime = null;
            }
        }

        for (var i = virtualMachines.Count - 1; i >= 0; i--)
        {
            if (!virtualMachines[i].Detected)
            {
                virtualMachines.RemoveAt(i);
            }
        }

        if (added)
        {
            virtualMachines.Sort(static (x, y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));
        }

        nextUpdate = DateTime.Now.AddMilliseconds(cacheDuration);
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private Measurement<int> MeasureCount()
    {
        lock (sync)
        {
            UpdateVirtualMachines();

            return new Measurement<int>(virtualMachines.Count, new KeyValuePair<string, object?>("host", host));
        }
    }

    private Measurement<int>[] MeasureInformation()
    {
        lock (sync)
        {
            UpdateVirtualMachines();

            var values = new Measurement<int>[virtualMachines.Count];

            for (var i = 0; i < virtualMachines.Count; i++)
            {
                var vm = virtualMachines[i];
                values[i] = new Measurement<int>(1, new("host", host), new("guid", vm.Guid), new("name", vm.Name), new("version", vm.Version));
            }

            return values;
        }
    }

    private List<Measurement<T>> Measure<T>(Func<VirtualMachine, T?> selector)
        where T : struct
    {
        lock (sync)
        {
            UpdateVirtualMachines();

            var values = new List<Measurement<T>>(virtualMachines.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var vm in virtualMachines)
            {
                var value = selector(vm);
                if (value.HasValue)
                {
                    values.Add(new Measurement<T>(value.Value, new("host", host), new("guid", vm.Guid), new("name", vm.Name)));
                }
            }

            return values;
        }
    }

    //--------------------------------------------------------------------------------
    // VirtualMachine
    //--------------------------------------------------------------------------------

    private sealed class VirtualMachine
    {
        public bool Detected { get; set; }

        public string Guid { get; }

        public string Name { get; set; } = default!;

        public string Version { get; set; } = default!;

        public int State { get; set; }

        public int? ProcessorLoad { get; set; }

        public double? MemoryUsage { get; set; }

        public double? Uptime { get; set; }

        public VirtualMachine(string guid)
        {
            Guid = guid;
        }
    }
}
