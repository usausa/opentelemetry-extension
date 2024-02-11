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

![Grafana](https://github.com/usausa/opentelemetry-extension/blob/main/Document/sensor.png)

## OpenTelemetryExtension.Instrumentation.HardwareMonitor

(TODO)
