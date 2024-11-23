namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

using System.Diagnostics.Metrics;
using System.Reflection;

using DeviceLib.SensorOmron;

using Microsoft.Extensions.Logging;

internal sealed class SensorOmronMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(SensorOmronMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly Device[] devices;

    private readonly Timer timer;

    public SensorOmronMetrics(
        ILogger<SensorOmronMetrics> log,
        SensorOmronOptions options)
    {
        log.InfoMetricsEnabled(nameof(SensorOmronMetrics));

        devices = options.Device.Select(static x => new Device(x)).ToArray();

        MeterInstance.CreateObservableGauge("sensor.temperature", () => Measure(static x => x.Temperature));
        MeterInstance.CreateObservableGauge("sensor.humidity", () => Measure(static x => x.Humidity));
        MeterInstance.CreateObservableGauge("sensor.light", () => Measure(static x => x.Light));
        MeterInstance.CreateObservableGauge("sensor.pressure", () => Measure(static x => x.Pressure));
        MeterInstance.CreateObservableGauge("sensor.noise", () => Measure(static x => x.Noise));
        MeterInstance.CreateObservableGauge("sensor.discomfort", () => Measure(static x => x.Discomfort));
        MeterInstance.CreateObservableGauge("sensor.heat", () => Measure(static x => x.Heat));
        MeterInstance.CreateObservableGauge("sensor.tvoc", () => Measure(static x => x.Etvoc));
        MeterInstance.CreateObservableGauge("sensor.co2", () => Measure(static x => x.Eco2));
        MeterInstance.CreateObservableGauge("sensor.seismic", () => Measure(static x => x.Seismic));

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
                values.Add(new Measurement<double>(value.Value, new("model", "rbt"), new("address", device.Setting.Port), new("name", device.Setting.Name)));
            }
        }

        return values;
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

#pragma warning disable IDE0032
    private sealed class Device : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

#if NET9_0_OR_GREATER
        private readonly Lock sync = new();
#else
        private readonly object sync = new();
#endif

        private readonly RbtSensorSerial sensor;

        private double? temperature;
        private double? humidity;
        private double? light;
        private double? pressure;
        private double? noise;
        private double? discomfort;
        private double? heat;
        private double? etvoc;
        private double? eco2;
        private double? seismic;

        public DeviceEntry Setting { get; }

        public double? Temperature
        {
            get
            {
                lock (sync)
                {
                    return temperature;
                }
            }
        }

        public double? Humidity
        {
            get
            {
                lock (sync)
                {
                    return humidity;
                }
            }
        }

        public double? Light
        {
            get
            {
                lock (sync)
                {
                    return light;
                }
            }
        }

        public double? Pressure
        {
            get
            {
                lock (sync)
                {
                    return pressure;
                }
            }
        }

        public double? Noise
        {
            get
            {
                lock (sync)
                {
                    return noise;
                }
            }
        }

        public double? Discomfort
        {
            get
            {
                lock (sync)
                {
                    return discomfort;
                }
            }
        }

        public double? Heat
        {
            get
            {
                lock (sync)
                {
                    return heat;
                }
            }
        }

        public double? Etvoc
        {
            get
            {
                lock (sync)
                {
                    return etvoc;
                }
            }
        }

        public double? Eco2
        {
            get
            {
                lock (sync)
                {
                    return eco2;
                }
            }
        }

        public double? Seismic
        {
            get
            {
                lock (sync)
                {
                    return seismic;
                }
            }
        }

        public Device(DeviceEntry setting)
        {
            Setting = setting;
            sensor = new RbtSensorSerial(setting.Port);
        }

        public void Dispose()
        {
            sensor.Dispose();
            semaphore.Dispose();
        }

#pragma warning disable CA1031
        public async ValueTask UpdateAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (!sensor.IsOpen())
                {
                    sensor.Open();
                }

                var result = await sensor.UpdateAsync();
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

                sensor.Close();
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
                temperature = sensor.Temperature;
                humidity = sensor.Humidity;
                light = sensor.Light;
                pressure = sensor.Pressure;
                noise = sensor.Noise;
                discomfort = sensor.Discomfort;
                heat = sensor.Heat;
                etvoc = sensor.Etvoc;
                eco2 = sensor.Eco2;
                seismic = sensor.Seismic;
            }
        }

        private void ClearValues()
        {
            lock (sensor)
            {
                temperature = null;
                humidity = null;
                light = null;
                pressure = null;
                noise = null;
                discomfort = null;
                heat = null;
                etvoc = null;
                eco2 = null;
                seismic = null;
            }
        }
    }
#pragma warning restore IDE0032
}
