# Telemetry Service

- Multiple metrics monitoring service

![Grafana](https://github.com/usausa/opentelemetry-extension/blob/main/Document/telemetry.png)

# RS-WFWATTCH2

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.WFWattch2)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.WFWattch2/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux|✅|

for [RS-WFWATTCH2](https://www.ratocsystems.com/products/sensor/watt/rswfwattch2/)

## Metrics

```
sensor_power
sensor_current
sensor_voltage
```

# SwitchBot Meter

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.SwitchBot.Windows)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.SwitchBot.Windows/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

for [SwitchBot Meter](https://www.switchbot.jp/products/switchbot-meter) and [SwitchBot Plug Mini](https://www.switchbot.jp/products/switchbot-plug-mini)

## Metrics

### Meter

```
sensor_rssi
sensor_temperature
sensor_humidity
sensor_co2
```

### Plug Mini

```
sensor_power
```

# 2JCIE-BU

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.SensorOmron)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.SensorOmron/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux|✅|

for [2JCIE-BU](https://www.fa.omron.co.jp/products/family/3724/lineup.html)

![Grafana](https://github.com/usausa/opentelemetry-extension/blob/main/Document/sensor.png)

## Metrics

```
sensor_temperature
sensor_humidity
sensor_light
sensor_pressure
sensor_noise
sensor_discomfort
sensor_heat
sensor_tvoc
sensor_co2
sensor_seismic
```

# Libre Hardware Monitor

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.HardwareMonitor)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.HardwareMonitor/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

## Metrics

```
hardware_battery_charge
hardware_battery_degradation
hardware_battery_voltage
hardware_battery_current
hardware_battery_capacity
hardware_battery_rate
hardware_battery_remaining
hardware_cpu_load
hardware_cpu_clock
hardware_cpu_temperature
hardware_cpu_voltage
hardware_cpu_current
hardware_cpu_power
hardware_gpu_load
hardware_gpu_clock
hardware_gpu_fan
hardware_gpu_temperature
hardware_gpu_power
hardware_gpu_memory
hardware_gpu_throughput
hardware_io_control
hardware_io_fan
hardware_io_temperature
hardware_io_voltage
hardware_memory_used
hardware_memory_available
hardware_memory_load
hardware_storage_used
hardware_storage_bytes
hardware_storage_speed
hardware_storage_temperature
hardware_storage_life
hardware_storage_amplification
hardware_storage_spare
hardware_network_bytes
hardware_network_speed
hardware_network_load
```
