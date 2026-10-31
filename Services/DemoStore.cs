using Google.Cloud.Firestore;
using PingKeeper.Models;

namespace PingKeeper.Services;

/// <summary>
/// In-memory store used in local/dev mode (no Google Cloud credentials needed).
/// Sites are read from the "Demo:Sites" configuration; pings are kept in memory.
/// </summary>
public class DemoStore : IMonitorStore
{
    private readonly object _gate = new();
    private readonly List<Site> _sites;
    private readonly List<PingRecord> _pings = new();

    public DemoStore(IConfiguration configuration)
    {
        var configured = configuration.GetSection("Demo:Sites").Get<List<Site>>() ?? new List<Site>();

        for (var i = 0; i < configured.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(configured[i].Id))
            {
                configured[i].Id = "site-" + (i + 1);
            }
        }

        _sites = configured;
    }

    public Task<List<Site>> GetSitesAsync(bool onlyEnabled = true, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var sites = _sites
                .Where(s => !onlyEnabled || s.Enabled)
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult(sites);
        }
    }

    public Task<Site?> GetSiteAsync(string siteId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_sites.FirstOrDefault(s => s.Id == siteId));
        }
    }

    public Task<Site> CreateSiteAsync(Site site, CancellationToken ct = default)
    {
        lock (_gate)
        {
            site.Id = Guid.NewGuid().ToString("N");
            site.CreatedAt = Timestamp.GetCurrentTimestamp();
            _sites.Add(site);
            return Task.FromResult(site);
        }
    }

    public Task UpdateSiteAsync(Site site, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var index = _sites.FindIndex(s => s.Id == site.Id);
            if (index >= 0)
            {
                _sites[index] = site;
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteSiteAsync(string siteId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _sites.RemoveAll(s => s.Id == siteId);
            _pings.RemoveAll(p => p.SiteId == siteId);
        }

        return Task.CompletedTask;
    }

    public Task AddPingAsync(PingRecord ping, CancellationToken ct = default)
    {
        ping.CheckedAt = Timestamp.GetCurrentTimestamp();
        lock (_gate)
        {
            _pings.Add(ping);
        }

        return Task.CompletedTask;
    }

    public Task<List<PingRecord>> GetRecentPingsAsync(
        string siteId, string type, int limit, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var result = _pings
                .Where(p => p.SiteId == siteId && p.Type == type)
                .OrderByDescending(p => p.CheckedAt.ToDateTimeOffset())
                .Take(limit)
                .ToList();
            return Task.FromResult(result);
        }
    }
}
