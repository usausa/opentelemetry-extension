namespace OpenTelemetryExtension.Instrumentation.SwitchBot.Windows;

public sealed class SwitchBotInstrumentationOptions
{
    public int TimeThreshold { get; set; } = 300_000;
}
