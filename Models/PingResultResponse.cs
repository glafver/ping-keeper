namespace PingKeeper.Models;

/// <summary>Response payload returned by GET /ping.</summary>
public class PingResultResponse
{
    public DateTimeOffset At { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<PingResultItem> Results { get; set; } = Array.Empty<PingResultItem>();
}

/// <summary>A single probe result in the /ping response.</summary>
public class PingResultItem
{
    public string Site { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public int Status { get; set; }
    public long Ms { get; set; }
    public string? Error { get; set; }
}
