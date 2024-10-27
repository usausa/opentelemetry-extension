namespace OpenTelemetryExtension.Instrumentation.SwitchBot.Windows;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddSwitchBotInstrumentation(this MeterProviderBuilder builder, Action<SwitchBotOptions> configure)
    {
        var options = new SwitchBotOptions();
        configure(options);

        builder.AddMeter(SwitchBotMetrics.MeterName);
        return builder.AddInstrumentation(() => new SwitchBotMetrics(options));
    }

    public static MeterProviderBuilder AddSwitchBotInstrumentation(this MeterProviderBuilder builder, SwitchBotOptions options)
    {
        builder.AddMeter(SwitchBotMetrics.MeterName);
        return builder.AddInstrumentation(() => new SwitchBotMetrics(options));
    }
}
