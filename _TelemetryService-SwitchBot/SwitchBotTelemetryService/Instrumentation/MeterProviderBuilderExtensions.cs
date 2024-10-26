namespace SwitchBotTelemetryService.Instrumentation;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddSwitchBotInstrumentation(this MeterProviderBuilder builder) =>
        AddSwitchBotInstrumentation(builder, _ => { });

    public static MeterProviderBuilder AddSwitchBotInstrumentation(this MeterProviderBuilder builder, Action<SwitchBotInstrumentationOptions> configure)
    {
        var options = new SwitchBotInstrumentationOptions();
        configure(options);

        builder.AddMeter(SwitchBotMetrics.MeterName);
        return builder.AddInstrumentation(() => new SwitchBotMetrics(options));
    }

    public static MeterProviderBuilder AddSwitchBotInstrumentation(this MeterProviderBuilder builder, SwitchBotInstrumentationOptions options)
    {
        builder.AddMeter(SwitchBotMetrics.MeterName);
        return builder.AddInstrumentation(() => new SwitchBotMetrics(options));
    }
}
