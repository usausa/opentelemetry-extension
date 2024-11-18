namespace OpenTelemetryExtension.Instrumentation.Ble;

using Microsoft.Extensions.Logging;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Metrics enabled. type=[{type}]")]
    public static partial void InfoMetricsEnabled(this ILogger logger, string type);
}
