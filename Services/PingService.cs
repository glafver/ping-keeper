using System.Diagnostics;
using PingKeeper.Models;

namespace PingKeeper.Services;

/// <summary>
/// Probes every enabled site at up to three levels (frontend, backend, database).
/// Each level can be toggled on/off per site; empty URLs are skipped.
/// </summary>
public class PingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMonitorStore _store;
    private readonly ILogger<PingService> _logger;

    public PingService(
        IHttpClientFactory httpClientFactory,
        IMonitorStore store,
        ILogger<PingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _store = store;
        _logger = logger;
    }

    public async Task<List<PingRecord>> PingAllAsync(CancellationToken ct = default)
    {
        var sites = await _store.GetSitesAsync(onlyEnabled: true, ct);
        var client = _httpClientFactory.CreateClient("ping");
        var results = new List<PingRecord>();

        foreach (var site in sites)
        {
            // Skip sites whose check interval hasn't elapsed yet.
            var interval = site.IntervalMinutes > 0 ? site.IntervalMinutes : IntervalDefaults.DefaultMinutes;
            var last = await GetLastPingTimeAsync(site, ct);
            if (last is not null)
            {
                var elapsed = DateTimeOffset.UtcNow - last.Value;
                if (elapsed < TimeSpan.FromMinutes(interval))
                {
                    _logger.LogInformation(
                        "Skipping {Site} — next check in ~{Minutes}m",
                        site.Name,
                        Math.Max(0, (int)(TimeSpan.FromMinutes(interval) - elapsed).TotalMinutes));
                    continue;
                }
            }

            // 1. Frontend.
            PingRecord? frontend = null;
            if (site.CheckFrontend)
            {
                frontend = await ProbeAsync(client, site, PingType.Frontend, site.Url, ct);
                await _store.AddPingAsync(frontend, ct);
                results.Add(frontend);
            }

            // 2. Backend.
            PingRecord? backend = null;
            if (site.CheckBackend && !string.IsNullOrWhiteSpace(site.ApiUrl))
            {
                backend = await ProbeAsync(client, site, PingType.Backend, site.ApiUrl, ct);
                await _store.AddPingAsync(backend, ct);
                results.Add(backend);
            }

            // 3. Database.
            PingRecord? database = null;
            if (site.CheckDatabase && !string.IsNullOrWhiteSpace(site.HealthUrl))
            {
                database = await ProbeAsync(client, site, PingType.Database, site.HealthUrl, ct);
                await _store.AddPingAsync(database, ct);
                results.Add(database);
            }

            _logger.LogInformation(
                "Pinged {Site} frontend={FE} backend={BE} database={DB}",
                site.Name,
                frontend is null ? "off" : frontend.Success ? "up" : "down",
                backend is null ? "off" : backend.Success ? "up" : "down",
                database is null ? "off" : database.Success ? "up" : "down");
        }

        return results;
    }

    /// <summary>Latest ping time across the levels that are actually enabled.</summary>
    private async Task<DateTimeOffset?> GetLastPingTimeAsync(Site site, CancellationToken ct)
    {
        var types = new List<PingType>();
        if (site.CheckFrontend) types.Add(PingType.Frontend);
        if (site.CheckBackend) types.Add(PingType.Backend);
        if (site.CheckDatabase) types.Add(PingType.Database);

        DateTimeOffset? last = null;
        foreach (var type in types)
        {
            var ping = (await _store.GetRecentPingsAsync(site.Id, type.ToStore(), 1, ct)).FirstOrDefault();
            if (ping is not null)
            {
                var dto = ping.CheckedAt.ToDateTimeOffset();
                if (last is null || dto > last) last = dto;
            }
        }

        return last;
    }

    private async Task<PingRecord> ProbeAsync(
        HttpClient client, Site site, PingType type, string url, CancellationToken ct)
    {
        var ping = new PingRecord
        {
            SiteId = site.Id,
            Type = type.ToStore(),
        };

        if (string.IsNullOrWhiteSpace(url))
        {
            ping.Success = false;
            ping.StatusCode = 0;
            ping.ResponseMs = -1;
            ping.Error = "URL not configured";
            return ping;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(url, ct);
            sw.Stop();

            ping.StatusCode = (int)response.StatusCode;
            ping.ResponseMs = sw.ElapsedMilliseconds;
            ping.Success = response.IsSuccessStatusCode;

            if (!ping.Success)
            {
                ping.Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            ping.StatusCode = 0;
            ping.ResponseMs = -1;
            ping.Success = false;
            ping.Error = ex.Message;
        }

        return ping;
    }
}
