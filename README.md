# OpenTelemetryExtension

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Metrics
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddHardwareMonitorInstrumentation()
            .AddSensorOmronInstrumentation("COM12");

        // http://localhost:9464/metrics
        metrics.AddPrometheusHttpListener();
    });

builder.Build().Run();
```


## OpenTelemetryExtension.Instrumentation.SensorOmron

[![NuGet Badge](https://buildstats.info/nuget/OpenTelemetryExtension.Instrumentation.SensorOmron)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.SensorOmron/)

for [2JCIE-BU](https://www.fa.omron.co.jp/products/family/3724/lineup.html)

![Grafana](https://github.com/usausa/opentelemetry-extension/blob/main/Document/sensor.png)

## OpenTelemetryExtension.Instrumentation.HardwareMonitor

[![NuGet Badge](https://buildstats.info/nuget/OpenTelemetryExtension.Instrumentation.HardwareMonitor)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.HardwareMonitor/)

- [X] Memory
- [X] Network
- [X] Storage (limited)
- [ ] Battery
- [X] I/O
- [ ] Cpu
- [ ] Gpu
