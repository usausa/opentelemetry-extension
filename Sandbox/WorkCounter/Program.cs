namespace WorkCounter;

using System.Diagnostics;

#pragma warning disable CA1823
// ReSharper disable UnusedMember.Local
internal class Program
{
    public static void Main()
    {
        var entries = Settings
            .Select(x => new CounterEntry
            {
                Counters = CreateCounters(x.Category, x.Counter, x.Instance).ToArray(),
                Setting = x
            })
            .ToArray();

        // Dummy
        foreach (var entry in entries)
        {
            foreach (var counter in entry.Counters)
            {
                counter.NextValue();
            }
        }

        foreach (var entry in entries)
        {
            Debug.WriteLine($"[{entry.Setting.Name}]");
            foreach (var counter in entry.Counters)
            {
                Debug.WriteLine($"  {counter.CategoryName}|{counter.CounterName}|{counter.InstanceName} : {counter.NextValue()}");
            }
        }
    }

    private static IEnumerable<PerformanceCounter> CreateCounters(string category, string counter, string? instance = null)
    {
        if (!String.IsNullOrEmpty(instance))
        {
            yield return new PerformanceCounter(category, counter, instance);
        }
        else
        {
            var pcc = new PerformanceCounterCategory(category);
            if (pcc.CategoryType == PerformanceCounterCategoryType.SingleInstance)
            {
                yield return new PerformanceCounter(category, counter);
            }
            else
            {
                var names = pcc.GetInstanceNames();
                Array.Sort(names);
                foreach (var name in names)
                {
                    yield return new PerformanceCounter(category, counter, name);
                }
            }
        }
    }

    // TODO 複数のカウンタをタグで集約することも検討？、単純にinstanceだけで良いか？

    private static readonly ObjectSetting[] Settings =
    [
        // プロセッサキュー
        new() { Category = "System", Counter = "Processor Queue Length", Name = "processor_queue" },
        // メモリページ/sec
        new() { Category = "Memory", Counter = "Pages/sec", Name = "memory_page" },
        // ディスク負荷(いらないか？)
        new() { Category = "LogicalDisk", Counter = "% Disk Time", Name = "disk_time" },
        // ディスクキュー長
        new() { Category = "PhysicalDisk", Counter = "Current Disk Queue Length", Name = "disk_queue" },
        // TCPコネクション
        new() { Category = "TCPv4", Counter = "Connections Established", Name = "tcp_connections_ip4" },
        new() { Category = "TCPv6", Counter = "Connections Established", Name = "tcp_connections_ip6" },
        // プロセス数、スレッド数
        new() { Category = "System", Counter = "Processes", Name = "process" },
        new() { Category = "Process", Counter = "Thread Count", Instance = "_Total", Name = "thread" },
        // ハンドル数
        new() { Category = "Process", Counter = "Handle Count", Instance = "_Total", Name = "handle" },
        // 稼働時間 1.1574074074074073e-005 // (1 / 86400.0) 直か？、Grafana側で変更
        new() { Category = "System", Counter = "System Up Time", Name = "uptime" }
    ];
}

internal sealed class CounterEntry
{
    public ObjectSetting Setting { get; set; } = default!;

    public PerformanceCounter[] Counters { get; set; } = default!;
}

internal sealed class ObjectSetting
{
    public string Name { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Counter { get; set; } = default!;
    public string? Instance { get; set; }
    //public double? Multiply { get; set; }
}
