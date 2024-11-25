# Telemetry Service

- Multiple metrics monitoring service

![Windows](https://github.com/usausa/opentelemetry-extension/blob/main/Document/windows.png)
![Summary](https://github.com/usausa/opentelemetry-extension/blob/main/Document/summary.png)
![Power](https://github.com/usausa/opentelemetry-extension/blob/main/Document/power.png)

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

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.SwitchBot)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.SwitchBot/)

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

![Environment](https://github.com/usausa/opentelemetry-extension/blob/main/Document/environment.png)

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

# S.M.A.R.T

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.DiskInfo)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.DiskInfo/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

![Environment](https://github.com/usausa/opentelemetry-extension/blob/main/Document/smart.png)

## Metrics

```
smart_disk_byte_per_sector
smart_nvme_value
smart_generic_value
```

# Performance Counter

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.PerformanceCounter)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.PerformanceCounter/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

## Metrics

```
performance_* (default)
```

# Hyper-V

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.HyperV)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.HyperV/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

## Metrics

```
hyperv_vm_count
hyperv_vm_information
hyperv_vm_state
hyperv_vm_processor_load
hyperv_vm_memory_usage
hyperv_vm_uptime
```

# Ping

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.Ping)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.Ping/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux|✅|

## Metrics

```
ping_result_time
```

# BLE signal strength

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.PerformanceCounter)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.PerformanceCounter/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

## WiFi signal strength

```
ble_rssi
```

# Performance Counter

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Instrumentation.Wifi)](https://www.nuget.org/packages/OpenTelemetryExtension.Instrumentation.Wifi/)

|OS|Support|
|:----|:----|
|Windows|✅|
|Linux||

## Metrics

```
wifi_rssi
```

# Install

## Windows

Copy files to install directory.

* TelemetryService.exe
* appsettings.json

```
sc create TelemetryService binPath=(install directory)\TelemetryService.exe start=auto
```

```
sc start TelemetryService
```

## Linux

## Prometheus

Add targets to prometheus.yml.

```yaml
  - job_name: 'telemetry'
    scrape_interval: 10s
    static_configs:
      - targets: ['192.168.1.101:9228']
```
