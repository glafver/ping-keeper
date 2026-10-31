using PingKeeper.Models;

namespace PingKeeper.Services;

/// <summary>
/// Builds the dashboard view model: per site, the latest frontend/backend/DB ping
/// plus uptime for each level.
/// </summary>
public class DashboardService
{
    // Uptime is the success rate over the last N checks.
    private const int UptimeWindowPings = 500;

    private readonly IMonitorStore _store;

    public DashboardService(IMonitorStore store)
    {
        _store = store;
    }

    public async Task<List<SiteStatus>> GetStatusesAsync(CancellationToken ct = default)
    {
        var sites = await _store.GetSitesAsync(onlyEnabled: false, ct);
        var result = new List<SiteStatus>(sites.Count);

        foreach (var site in sites)
        {
            var frontend = await _store.GetRecentPingsAsync(site.Id, PingType.Frontend.ToStore(), UptimeWindowPings, ct);
            var backend = await _store.GetRecentPingsAsync(site.Id, PingType.Backend.ToStore(), UptimeWindowPings, ct);
            var database = await _store.GetRecentPingsAsync(site.Id, PingType.Database.ToStore(), UptimeWindowPings, ct);

            result.Add(new SiteStatus
            {
                Site = site,
                Frontend = frontend.FirstOrDefault(),
                Backend = backend.FirstOrDefault(),
                Database = database.FirstOrDefault(),
                FrontendUptime = CalculateUptime(frontend),
                BackendUptime = CalculateUptime(backend),
                DatabaseUptime = CalculateUptime(database),
            });
        }

        return result;
    }

    private static double CalculateUptime(IReadOnlyCollection<PingRecord> pings)
    {
        if (pings.Count == 0)
        {
            return 0;
        }

        var ok = pings.Count(p => p.Success);
        return Math.Round(ok * 100.0 / pings.Count, 2);
    }
}
