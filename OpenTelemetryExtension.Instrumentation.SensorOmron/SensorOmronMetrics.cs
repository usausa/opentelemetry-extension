namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

using System.Diagnostics.Metrics;
using System.Reflection;

using DeviceLib.SensorOmron;

internal sealed class SensorOmronMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(SensorOmronMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string name;

    private readonly RbtSensorSerial sensor;

    private readonly Timer timer;

    private readonly SemaphoreSlim semaphore = new(1, 1);

    private readonly object sync = new();

    private float? temperature;
    private float? humidity;
    private float? light;
    private float? pressure;
    private float? noise;
    private float? discomfort;
    private float? heat;
    private float? etvoc;
    private float? eco2;
    private float? seismic;

    public SensorOmronMetrics(SensorOmronInstrumentationOptions options)
    {
        name = options.Port;
        sensor = new RbtSensorSerial(options.Port);

        MeterInstance.CreateObservableUpDownCounter("sensor.temperature", () => ToMeasurement(static x => x.temperature));
        MeterInstance.CreateObservableUpDownCounter("sensor.humidity", () => ToMeasurement(static x => x.humidity));
        MeterInstance.CreateObservableUpDownCounter("sensor.light", () => ToMeasurement(static x => x.light));
        MeterInstance.CreateObservableUpDownCounter("sensor.pressure", () => ToMeasurement(static x => x.pressure));
        MeterInstance.CreateObservableUpDownCounter("sensor.noise", () => ToMeasurement(static x => x.noise));
        MeterInstance.CreateObservableUpDownCounter("sensor.discomfort", () => ToMeasurement(static x => x.discomfort));
        MeterInstance.CreateObservableUpDownCounter("sensor.heat", () => ToMeasurement(static x => x.heat));
        MeterInstance.CreateObservableUpDownCounter("sensor.etvoc", () => ToMeasurement(static x => x.etvoc));
        MeterInstance.CreateObservableUpDownCounter("sensor.eco2", () => ToMeasurement(static x => x.eco2));
        MeterInstance.CreateObservableUpDownCounter("sensor.seismic", () => ToMeasurement(static x => x.seismic));

        timer = new Timer(_ => Update(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    private Measurement<double>[] ToMeasurement(Func<SensorOmronMetrics, double?> selector)
    {
        lock (sync)
        {
            var value = selector(this);
            if (value.HasValue)
            {
                return [new Measurement<double>(value.Value, new("model", "rbt"), new("id", name))];
            }
            return [];
        }
    }

    public void Dispose()
    {
        timer.Dispose();
        sensor.Dispose();
        semaphore.Dispose();
    }

#pragma warning disable CA1031
    private async void Update()
    {
        await semaphore.WaitAsync();
        try
        {
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
        lock (sync)
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
