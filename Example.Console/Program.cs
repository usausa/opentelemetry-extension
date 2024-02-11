using OpenTelemetry.Metrics;

using Serilog;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

// Service
builder.Services
    .AddWindowsService()
    .AddSystemd();

// Logging
builder.Logging.ClearProviders();
builder.Services.AddSerilog(options =>
{
    options.ReadFrom.Configuration(builder.Configuration);
});

// Metrics
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        // TODO
        //metrics
        //    .AddHadwareMonitorInstrumentation()
        //    .AddSendorOmronInstrumentation();

        // http://localhost:9464/metrics
        metrics.AddPrometheusHttpListener();
    });

var host = builder.Build();

host.Run();
