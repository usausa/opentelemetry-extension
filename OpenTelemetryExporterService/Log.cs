namespace OpenTelemetryExporterService;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Service start.")]
    public static partial void InfoServiceStart(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment: version=[{version}], runtime=[{runtime}], directory=[{directory}]")]
    public static partial void InfoServiceSettingsEnvironment(this ILogger logger, Version? version, Version runtime, string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Metrics: application=[{application}], hardware=[{hardware}], sensor=[{sensor}]")]
    public static partial void InfoServiceSettingsMetrics(this ILogger logger, bool application, bool hardware, bool sensor);
}
