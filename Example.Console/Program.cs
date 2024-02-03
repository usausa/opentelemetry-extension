using OpenTelemetry.Metrics;

var builder = Host.CreateApplicationBuilder(args);

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
