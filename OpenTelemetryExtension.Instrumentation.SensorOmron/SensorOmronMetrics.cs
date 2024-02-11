namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

using System.Diagnostics.Metrics;
using System.Reflection;

internal sealed class SensorOmronMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(SensorOmronMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly Sensor sensor;

    private readonly Timer timer;

    public SensorOmronMetrics(SensorOmronInstrumentationOptions options)
    {
        sensor = new Sensor(options.Port);
        sensor.Update();

        MeterInstance.CreateObservableUpDownCounter("sensor.temperature", () => ToMeasurement(sensor.Temperature));
        MeterInstance.CreateObservableUpDownCounter("sensor.humidity", () => ToMeasurement(sensor.Humidity));
        MeterInstance.CreateObservableUpDownCounter("sensor.pressure", () => ToMeasurement(sensor.Pressure));
        MeterInstance.CreateObservableUpDownCounter("sensor.noise", () => ToMeasurement(sensor.Noise));
        MeterInstance.CreateObservableUpDownCounter("sensor.discomfort", () => ToMeasurement(sensor.Discomfort));
        MeterInstance.CreateObservableUpDownCounter("sensor.heat", () => ToMeasurement(sensor.Heat));
        MeterInstance.CreateObservableUpDownCounter("sensor.etvoc", () => ToMeasurement(sensor.Etvoc));
        MeterInstance.CreateObservableUpDownCounter("sensor.eco2", () => ToMeasurement(sensor.Eco2));
        MeterInstance.CreateObservableUpDownCounter("sensor.seismic", () => ToMeasurement(sensor.Seismic));

        timer = new Timer(Update, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(options.Interval));
    }

    private static Measurement<double>[] ToMeasurement(double? value) =>
        value.HasValue ? [new Measurement<double>(value.Value)] : [];

    public void Dispose()
    {
        timer.Dispose();
    }

    private void Update(object? state)
    {
        sensor.Update();
    }
}
