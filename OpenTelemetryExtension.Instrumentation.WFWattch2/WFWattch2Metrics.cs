namespace OpenTelemetryExtension.Instrumentation.WFWattch2;

using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;

using DeviceLib.WFWattch2;

using Microsoft.Extensions.Logging;

internal sealed class WFWattch2Metrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(WFWattch2Metrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly Device[] devices;

    private readonly Timer timer;

    public WFWattch2Metrics(
        ILogger<WFWattch2Metrics> log,
        WFWattch2Options options)
    {
        log.InfoMetricsEnabled(nameof(WFWattch2Metrics));

        devices = options.Device.Select(static x => new Device(x)).ToArray();

        MeterInstance.CreateObservableUpDownCounter("sensor.power", () => Measure(static x => x.Power));
        MeterInstance.CreateObservableUpDownCounter("sensor.current", () => Measure(static x => x.Current));
        MeterInstance.CreateObservableUpDownCounter("sensor.voltage", () => Measure(static x => x.Voltage));

        timer = new Timer(_ => Update(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    public void Dispose()
    {
        timer.Dispose();
        foreach (var device in devices)
        {
            device.Dispose();
        }
    }

    private void Update()
    {
        foreach (var device in devices)
        {
            _ = Task.Run(async () => await device.UpdateAsync());
        }
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private List<Measurement<double>> Measure(Func<Device, double?> selector)
    {
        var values = new List<Measurement<double>>(devices.Length);

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var device in devices)
        {
            var value = selector(device);
            if (value.HasValue)
            {
                values.Add(new Measurement<double>(value.Value, new("model", "wfwatch2"), new("address", device.Setting.Address), new("name", device.Setting.Name)));
            }
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private sealed class Device : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

#if NET9_0_OR_GREATER
        private readonly Lock sync = new();
#else
        private readonly object sync = new();
#endif

        private readonly WattchClient client;

#pragma warning disable IDE0032
        private double? power;
        private double? voltage;
        private double? current;
#pragma warning restore IDE0032

        public DeviceEntry Setting { get; }

        public double? Power
        {
            get
            {
                lock (sync)
                {
                    return power;
                }
            }
        }

        public double? Voltage
        {
            get
            {
                lock (sync)
                {
                    return voltage;
                }
            }
        }

        public double? Current
        {
            get
            {
                lock (sync)
                {
                    return current;
                }
            }
        }

        public Device(DeviceEntry setting)
        {
            Setting = setting;
            client = new WattchClient(IPAddress.Parse(setting.Address));
        }

        public void Dispose()
        {
            client.Dispose();
            semaphore.Dispose();
        }

#pragma warning disable CA1031
        public async ValueTask UpdateAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (!client.IsConnected())
                {
                    await client.ConnectAsync();
                }

                var result = await client.UpdateAsync();
                if (result)
                {
                    ReadValues();
                }
                else
                {
                    ClearValues();
                }
            }
            catch
            {
                ClearValues();

                client.Close();
            }
            finally
            {
                semaphore.Release();
            }
        }
#pragma warning restore CA1031

        private void ReadValues()
        {
            lock (sync)
            {
                power = client.Power;
                voltage = client.Voltage;
                current = client.Current;
            }
        }

        private void ClearValues()
        {
            lock (sync)
            {
                power = null;
                voltage = null;
                current = null;
            }
        }
    }
}
