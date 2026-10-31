using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingKeeper.Models;
using PingKeeper.Services;

namespace PingKeeper.Controllers;

/// <summary>Site management UI: add, edit, toggle, delete, and per-site history.</summary>
public class SitesController : Controller
{
    private const int HistoryLimit = 100;

    private readonly IMonitorStore _store;

    public SitesController(IMonitorStore store)
    {
        _store = store;
    }

    [HttpGet("/sites/new")]
    public IActionResult New()
    {
        return View(new SiteInput());
    }

    [HttpPost("/sites")]
    public async Task<IActionResult> Create(SiteInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("New", input);
        }

        var site = new Site
        {
            Name = input.Name.Trim(),
            Url = input.Url.Trim(),
            ApiUrl = input.ApiUrl?.Trim() ?? string.Empty,
            HealthUrl = input.HealthUrl?.Trim() ?? string.Empty,
            Enabled = true,
            IntervalMinutes = IntervalDefaults.ToMinutes(input.IntervalValue, input.IntervalUnit),
            CheckFrontend = input.CheckFrontend,
            CheckBackend = input.CheckBackend,
            CheckDatabase = input.CheckDatabase,
        };

        await _store.CreateSiteAsync(site, ct);
        return Redirect("/");
    }

    [HttpGet("/sites/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        var site = await _store.GetSiteAsync(id, ct);
        if (site is null)
        {
            return NotFound();
        }

        var frontend = await _store.GetRecentPingsAsync(id, PingType.Frontend.ToStore(), HistoryLimit, ct);
        var backend = await _store.GetRecentPingsAsync(id, PingType.Backend.ToStore(), HistoryLimit, ct);
        var database = await _store.GetRecentPingsAsync(id, PingType.Database.ToStore(), HistoryLimit, ct);
        var pings = frontend
            .Concat(backend)
            .Concat(database)
            .OrderByDescending(p => p.CheckedAt.ToDateTimeOffset())
            .Take(HistoryLimit)
            .ToList();

        var model = new SiteDetailModel { Site = site, Pings = pings };

        var latencies = pings
            .Where(p => p.Success && p.ResponseMs >= 0)
            .Select(p => p.ResponseMs)
            .OrderBy(x => x)
            .ToList();

        if (latencies.Count > 0)
        {
            model.Checks = latencies.Count;
            model.AvgMs = (long)Math.Round(latencies.Average());
            model.MinMs = latencies[0];
            model.MaxMs = latencies[^1];
            var p95Index = Math.Max(0, (int)Math.Ceiling(latencies.Count * 0.95) - 1);
            model.P95Ms = latencies[p95Index];
        }

        return View(model);
    }

    [HttpGet("/sites/{id}/edit")]
    public async Task<IActionResult> Edit(string id, CancellationToken ct)
    {
        var site = await _store.GetSiteAsync(id, ct);
        if (site is null)
        {
            return NotFound();
        }

        var (intervalValue, intervalUnit) = IntervalDefaults.FromMinutes(site.IntervalMinutes);

        return View(new SiteEditModel
        {
            Site = site,
            Input = new SiteInput
            {
                Name = site.Name,
                Url = site.Url,
                ApiUrl = site.ApiUrl,
                HealthUrl = site.HealthUrl,
                IntervalValue = intervalValue,
                IntervalUnit = intervalUnit,
                CheckFrontend = site.CheckFrontend,
                CheckBackend = site.CheckBackend,
                CheckDatabase = site.CheckDatabase,
            },
        });
    }

    [HttpPost("/sites/{id}")]
    public async Task<IActionResult> Update(string id, SiteInput input, CancellationToken ct)
    {
        var site = await _store.GetSiteAsync(id, ct);
        if (site is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", new SiteEditModel { Site = site, Input = input });
        }

        site.Name = input.Name.Trim();
        site.Url = input.Url.Trim();
        site.ApiUrl = input.ApiUrl?.Trim() ?? string.Empty;
        site.HealthUrl = input.HealthUrl?.Trim() ?? string.Empty;
        site.IntervalMinutes = IntervalDefaults.ToMinutes(input.IntervalValue, input.IntervalUnit);
        site.CheckFrontend = input.CheckFrontend;
        site.CheckBackend = input.CheckBackend;
        site.CheckDatabase = input.CheckDatabase;
        await _store.UpdateSiteAsync(site, ct);

        return Redirect("/");
    }

    [HttpPost("/sites/{id}/toggle")]
    public async Task<IActionResult> Toggle(string id, CancellationToken ct)
    {
        var site = await _store.GetSiteAsync(id, ct);
        if (site is null)
        {
            return NotFound();
        }

        site.Enabled = !site.Enabled;
        await _store.UpdateSiteAsync(site, ct);
        return Redirect("/");
    }

    [HttpPost("/sites/{id}/delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _store.DeleteSiteAsync(id, ct);
        return Redirect("/");
    }
}
