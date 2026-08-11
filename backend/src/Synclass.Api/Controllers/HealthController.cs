using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;

namespace Synclass.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check requisitado");

        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        return Ok(new HealthResponse("ok", trackId));
    }
}

public sealed record HealthResponse(string Status, string TrackId);
