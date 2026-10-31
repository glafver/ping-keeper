namespace PingKeeper.Models;

/// <summary>View model for the per-site history page.</summary>
public class SiteDetailModel
{
    public Site Site { get; set; } = new();
    public List<PingRecord> Pings { get; set; } = new();

    // Response-time stats over the successful checks in Pings.
    public int Checks { get; set; }
    public long AvgMs { get; set; }
    public long MinMs { get; set; }
    public long MaxMs { get; set; }
    public long P95Ms { get; set; }
}
