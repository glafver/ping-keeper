using Google.Cloud.Firestore;

namespace PingKeeper.Models;

/// <summary>
/// A monitored site, stored in the "sites" collection.
/// </summary>
[FirestoreData]
public class Site
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name shown on the dashboard.</summary>
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    /// <summary>Frontend URL to ping (e.g. https://app.example.com).</summary>
    [FirestoreProperty]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Backend API URL to ping (e.g. https://app.example.com/api/ping). Optional —
    /// used when the frontend and the API/backend live behind different endpoints.
    /// </summary>
    [FirestoreProperty]
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Health URL that performs a real DB query (e.g. https://app.example.com/api/health
    /// returning "SELECT 1"). Pinging this keeps the underlying DB from sleeping.
    /// </summary>
    [FirestoreProperty]
    public string HealthUrl { get; set; } = string.Empty;

    /// <summary>When false the site is skipped by /ping.</summary>
    [FirestoreProperty]
    public bool Enabled { get; set; } = true;

    /// <summary>How often to ping this site, in minutes (default 1440 = once a day).</summary>
    [FirestoreProperty]
    public int IntervalMinutes { get; set; } = IntervalDefaults.DefaultMinutes;

    /// <summary>Whether to probe the frontend URL.</summary>
    [FirestoreProperty]
    public bool CheckFrontend { get; set; } = true;

    /// <summary>Whether to probe the backend API URL.</summary>
    [FirestoreProperty]
    public bool CheckBackend { get; set; } = true;

    /// <summary>Whether to probe the DB health URL.</summary>
    [FirestoreProperty]
    public bool CheckDatabase { get; set; } = true;

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}
