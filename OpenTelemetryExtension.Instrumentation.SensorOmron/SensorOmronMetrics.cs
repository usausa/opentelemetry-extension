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

        MeterInstance.CreateObservableUpDownCounter("sensor.temperature", () => GatherMeasurement(static x => x.Temperature));
        MeterInstance.CreateObservableUpDownCounter("sensor.humidity", () => GatherMeasurement(static x => x.Humidity));
        MeterInstance.CreateObservableUpDownCounter("sensor.light", () => GatherMeasurement(static x => x.Light));
        MeterInstance.CreateObservableUpDownCounter("sensor.pressure", () => GatherMeasurement(static x => x.Pressure));
        MeterInstance.CreateObservableUpDownCounter("sensor.noise", () => GatherMeasurement(static x => x.Noise));
        MeterInstance.CreateObservableUpDownCounter("sensor.discomfort", () => GatherMeasurement(static x => x.Discomfort));
        MeterInstance.CreateObservableUpDownCounter("sensor.heat", () => GatherMeasurement(static x => x.Heat));
        MeterInstance.CreateObservableUpDownCounter("sensor.tvoc", () => GatherMeasurement(static x => x.Etvoc));
        MeterInstance.CreateObservableUpDownCounter("sensor.co2", () => GatherMeasurement(static x => x.Eco2));
        MeterInstance.CreateObservableUpDownCounter("sensor.seismic", () => GatherMeasurement(static x => x.Seismic));

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

    private List<Measurement<double>> GatherMeasurement(Func<Device, double?> selector)
    {
        var values = new List<Measurement<double>>(devices.Length);

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var device in devices)
        {
            var value = selector(device);
            if (value.HasValue)
            {
                values.Add(new Measurement<double>(
                    value.Value,
                    new("model", "rbt"),
                    new("address", device.Setting.Port),
                    new("name", device.Setting.Name)));
            }
        }

        return values;
    }

    private void Update()
    {
        foreach (var device in devices)
        {
            _ = Task.Run(async () => await device.UpdateAsync());
        }
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private sealed class Device : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

        private readonly object sync = new();

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
}
