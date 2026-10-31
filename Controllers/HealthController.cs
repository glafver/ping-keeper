using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingKeeper.Controllers;

/// <summary>Liveness probe for Cloud Run and other orchestrators.</summary>
[AllowAnonymous]
[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/healthz")]
    public IActionResult Healthz() => Ok(new { status = "ok" });
}
