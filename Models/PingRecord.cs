using Google.Cloud.Firestore;

namespace PingKeeper.Models;

/// <summary>
/// A single probe result, stored in the "pings" collection.
/// </summary>
[FirestoreData]
public class PingRecord
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Reference to Site.Id.</summary>
    [FirestoreProperty]
    public string SiteId { get; set; } = string.Empty;

    /// <summary>"frontend" (Site.Url) or "database" (Site.HealthUrl).</summary>
    [FirestoreProperty]
    public string Type { get; set; } = "frontend";

    /// <summary>HTTP status code, or 0 when the request failed.</summary>
    [FirestoreProperty]
    public int StatusCode { get; set; }

    /// <summary>True when the response was 2xx.</summary>
    [FirestoreProperty]
    public bool Success { get; set; }

    /// <summary>Round-trip time in milliseconds, or -1 on failure.</summary>
    [FirestoreProperty]
    public long ResponseMs { get; set; } = -1;

    /// <summary>Error message when the request failed, otherwise null.</summary>
    [FirestoreProperty]
    public string? Error { get; set; }

    [FirestoreProperty]
    public Timestamp CheckedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}
