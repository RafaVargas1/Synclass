using Serilog;
using Serilog.Formatting.Compact;

namespace Synclass.Api.Logging;

/// <summary>
/// Configura Serilog para emitir JSON estruturado no console, incluindo o
/// TrackId (ver <see cref="Middleware.TrackIdMiddleware"/>) em cada entrada
/// de log gerada durante uma requisição.
/// </summary>
public static class SerilogSetup
{
    public static void UseStructuredJsonLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Synclass.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());
        });
    }
}
