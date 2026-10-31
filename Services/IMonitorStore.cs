using PingKeeper.Models;

namespace PingKeeper.Services;

/// <summary>
/// Abstraction over the ping/site persistence so the app can run against
/// Firestore (production) or an in-memory demo store (local development).
/// </summary>
public interface IMonitorStore
{
    Task<List<Site>> GetSitesAsync(bool onlyEnabled = true, CancellationToken ct = default);
    Task<Site?> GetSiteAsync(string siteId, CancellationToken ct = default);
    Task<Site> CreateSiteAsync(Site site, CancellationToken ct = default);
    Task UpdateSiteAsync(Site site, CancellationToken ct = default);
    Task DeleteSiteAsync(string siteId, CancellationToken ct = default);
    Task AddPingAsync(PingRecord ping, CancellationToken ct = default);
    Task<List<PingRecord>> GetRecentPingsAsync(string siteId, string type, int limit, CancellationToken ct = default);
}
