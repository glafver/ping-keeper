using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingKeeper.Services;

namespace PingKeeper.Controllers;

/// <summary>Public read-only dashboard + about page.</summary>
public class DashboardController : Controller
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("/")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var statuses = await _dashboard.GetStatusesAsync(ct);
        return View(statuses);
    }

    [HttpGet("/about")]
    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }
}
