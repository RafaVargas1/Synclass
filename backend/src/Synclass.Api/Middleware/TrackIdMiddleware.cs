using Serilog.Context;

namespace Synclass.Api.Middleware;

/// <summary>
/// Garante que toda requisição carregue um TrackId (correlation id) propagado
/// em todos os logs emitidos durante seu processamento e devolvido ao cliente
/// via header, permitindo seguir um fluxo ponta a ponta entre app e servidor.
/// </summary>
public sealed class TrackIdMiddleware
{
    public const string HeaderName = "X-Track-Id";

    private readonly RequestDelegate _next;

    public TrackIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var trackId = ResolveTrackId(context);

        context.Response.Headers[HeaderName] = trackId;

        using (LogContext.PushProperty("TrackId", trackId))
        {
            await _next(context);
        }
    }

    private static string ResolveTrackId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming) &&
            !string.IsNullOrWhiteSpace(incoming))
        {
            return incoming.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}

public static class TrackIdMiddlewareExtensions
{
    public static IApplicationBuilder UseTrackId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TrackIdMiddleware>();
    }
}
