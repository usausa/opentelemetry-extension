namespace OpenTelemetryExtension.Instrumentation.HyperV;

#pragma warning disable CA1819
public sealed class HyperVOptions
{
    public int CacheDuration { get; set; } = 500;

    public string? IgnoreExpression { get; set; }

    public string Host { get; set; } = default!;
}
#pragma warning restore CA1819
