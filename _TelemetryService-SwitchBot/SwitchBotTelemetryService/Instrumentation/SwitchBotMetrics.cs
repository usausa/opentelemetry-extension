namespace SwitchBotTelemetryService.Instrumentation;

using System.Diagnostics.Metrics;
using System.Reflection;

using InTheHand.Bluetooth;

internal sealed class SwitchBotMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(SwitchBotMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly SwitchBotInstrumentationOptions options;

    private readonly SortedDictionary<string, Data> sensorData = [];

    public SwitchBotMetrics(SwitchBotInstrumentationOptions options)
    {
        this.options = options;

        MeterInstance.CreateObservableUpDownCounter(
            "sensor.rssi",
            () => GatherValues(static _ => true, ToRssi));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.temperature",
            () => GatherValues(static _ => true, ToTemperature));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.humidity",
            () => GatherValues(static _ => true, ToHumidity));
        MeterInstance.CreateObservableUpDownCounter(
            "sensor.co2",
            () => GatherValues(static x => x.Co2.HasValue, ToCo2));

        Bluetooth.AdvertisementReceived += BluetoothOnAdvertisementReceived;
        _ = Bluetooth.RequestLEScanAsync();
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void BluetoothOnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent e)
    {
        if (e.ManufacturerData.TryGetValue(0x0969, out var buffer) && buffer.Length >= 11)
        {
            var temperature = (((double)(buffer[8] & 0x0f) / 10) + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
            var humidity = buffer[10] & 0x7f;

            lock (sensorData)
            {
                if (!sensorData.TryGetValue(e.Device.Id, out var data))
                {
                    data = new Data { Id = e.Device.Id };
                    sensorData[e.Device.Id] = data;
                }

                data.LastUpdate = DateTime.Now;
                data.Rssi = e.Rssi;
                data.Temperature = temperature;
                data.Humidity = humidity;
                // TODO
                data.Co2 = null;
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    private Measurement<double>[] GatherValues(Func<Data, bool> selector, Func<Data, Measurement<double>> converter)
    {
        var list = new List<Data>();

        lock (sensorData)
        {
            var removes = default(List<string>?);

            var now = DateTime.Now;
            foreach (var (key, data) in sensorData)
            {
                if ((now - data.LastUpdate).TotalSeconds > options.TimeThreshold)
                {
                    removes ??= [];
                    removes.Add(key);
                }
                else if (selector(data))
                {
                    list.Add(data);
                }
            }

            if (removes is not null)
            {
                foreach (var key in removes)
                {
                    sensorData.Remove(key);
                }
            }
        }

        var values = new Measurement<double>[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            values[i] = converter(list[i]);
        }
        return values;
    }

    private static Measurement<double> ToRssi(Data data) =>
        new(data.Rssi, new("type", "switchbot"), new("device", data.Id));

    private static Measurement<double> ToTemperature(Data data) =>
        new(data.Temperature, new("type", "switchbot"), new("device", data.Id));

    private static Measurement<double> ToHumidity(Data data) =>
        new(data.Humidity, new("type", "switchbot"), new("device", data.Id));

    private static Measurement<double> ToCo2(Data data) =>
        new(data.Co2!.Value, new("type", "switchbot"), new("device", data.Id));

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    private sealed class Data
    {
        public required string Id { get; init; }

        public DateTime LastUpdate { get; set; }

        public double Rssi { get; set; }

        public double Temperature { get; set; }

        public double Humidity { get; set; }

        public double? Co2 { get; set; }
    }
}
