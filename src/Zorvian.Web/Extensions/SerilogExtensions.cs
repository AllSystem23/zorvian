using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Zorvian.Web.Extensions;

public static class SerilogExtensions
{
    public static IHostBuilder UseZorvianSerilog(this IHostBuilder hostBuilder, IConfiguration config)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                .MinimumLevel.Override("MassTransit", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Zorvian-ERP")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // Elasticsearch sink — only if configured
            var esUri = config["Elasticsearch:Uri"];
            if (!string.IsNullOrEmpty(esUri))
            {
                var indexFormat = config["Elasticsearch:IndexFormat"] ?? "zorvian-logs-{0:yyyy.MM.dd}";
                var autoRegister = config.GetValue<bool>("Elasticsearch:AutoRegisterTemplate");

                configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUri))
                {
                    AutoRegisterTemplate = autoRegister,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
                    IndexFormat = indexFormat,
                    NumberOfShards = 2,
                    NumberOfReplicas = 1,
                    BatchAction = ElasticOpType.Create,
                    CustomFormatter = new Serilog.Formatting.Elasticsearch.ElasticsearchJsonFormatter(),
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
                    MinimumLogEventLevel = LogEventLevel.Information
                });
            }

            // File sink for local logs
            var logPath = config["Logging:FilePath"] ?? "logs/zorvian-.log";
            configuration.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        });

        return hostBuilder;
    }
}
