using Google.Cloud.Firestore;
using PingKeeper.Models;

namespace PingKeeper.Services;

/// <summary>
/// Thin wrapper around Firestore for the "sites" and "pings" collections.
/// </summary>
public class FirestoreService : IMonitorStore
{
    public const string SitesCollection = "sites";
    public const string PingsCollection = "pings";

    private readonly FirestoreDb _db;

    public FirestoreService(FirestoreDb db)
    {
        _db = db;
    }

    public CollectionReference Sites => _db.Collection(SitesCollection);
    public CollectionReference Pings => _db.Collection(PingsCollection);

    public async Task<List<Site>> GetSitesAsync(bool onlyEnabled = true, CancellationToken ct = default)
    {
        Query query = Sites;
        if (onlyEnabled)
        {
            query = query.WhereEqualTo(nameof(Site.Enabled), true);
        }

        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents
            .Select(d => d.ConvertTo<Site>())
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<Site?> GetSiteAsync(string siteId, CancellationToken ct = default)
    {
        var snapshot = await Sites.Document(siteId).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<Site>() : null;
    }

    public async Task<Site> CreateSiteAsync(Site site, CancellationToken ct = default)
    {
        site.CreatedAt = Timestamp.GetCurrentTimestamp();
        var doc = await Sites.AddAsync(site, ct);
        site.Id = doc.Id;
        return site;
    }

    public async Task UpdateSiteAsync(Site site, CancellationToken ct = default)
    {
        await Sites.Document(site.Id).SetAsync(site, SetOptions.MergeAll, ct);
    }

    public async Task DeleteSiteAsync(string siteId, CancellationToken ct = default)
    {
        await Sites.Document(siteId).DeleteAsync(cancellationToken: ct);
    }

    public async Task AddPingAsync(PingRecord ping, CancellationToken ct = default)
    {
        ping.CheckedAt = Timestamp.GetCurrentTimestamp();
        await Pings.AddAsync(ping, ct);
    }

    /// <summary>
    /// Recent pings for a site and type, newest first.
    /// Requires composite index (SiteId ASC, Type ASC, CheckedAt DESC) — see firestore.indexes.json.
    /// </summary>
    public async Task<List<PingRecord>> GetRecentPingsAsync(
        string siteId, string type, int limit, CancellationToken ct = default)
    {
        var snapshot = await Pings
            .WhereEqualTo(nameof(PingRecord.SiteId), siteId)
            .WhereEqualTo(nameof(PingRecord.Type), type)
            .OrderByDescending(nameof(PingRecord.CheckedAt))
            .Limit(limit)
            .GetSnapshotAsync(ct);

        return snapshot.Documents.Select(d => d.ConvertTo<PingRecord>()).ToList();
    }
}
