namespace PingKeeper.Models;

/// <summary>Dashboard view model for a single site.</summary>
public class SiteStatus
{
    public Site Site { get; set; } = new();
    public PingRecord? Frontend { get; set; }
    public PingRecord? Backend { get; set; }
    public PingRecord? Database { get; set; }
    public double FrontendUptime { get; set; }
    public double BackendUptime { get; set; }
    public double DatabaseUptime { get; set; }
}
