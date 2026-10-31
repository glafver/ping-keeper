using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingKeeper.Models;
using PingKeeper.Services;

namespace PingKeeper.Controllers;

/// <summary>
/// Triggered by Cloud Scheduler on a schedule. Probes all sites and writes results.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    private readonly PingService _pingService;
    private readonly IConfiguration _configuration;

    public PingController(PingService pingService, IConfiguration configuration)
    {
        _pingService = pingService;
        _configuration = configuration;
    }

    [HttpGet("/ping")]
    [AllowAnonymous] // Cloud Scheduler calls this without a browser session.
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        // Optional shared secret to protect the public endpoint.
        // If set, Cloud Scheduler must send it via the X-Ping-Secret header.
        var secret = _configuration["Ping:Secret"];
        if (!string.IsNullOrEmpty(secret))
        {
            var provided = Request.Headers["X-Ping-Secret"].ToString();
            if (!string.Equals(provided, secret, StringComparison.Ordinal))
            {
                return Unauthorized();
            }
        }

        var results = await _pingService.PingAllAsync(ct);

        return Ok(new PingResultResponse
        {
            At = DateTimeOffset.UtcNow,
            Count = results.Count,
            Results = results.Select(r => new PingResultItem
            {
                Site = r.SiteId,
                Type = r.Type,
                Ok = r.Success,
                Status = r.StatusCode,
                Ms = r.ResponseMs,
                Error = r.Error,
            }).ToList(),
        });
    }
}
