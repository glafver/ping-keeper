namespace PingKeeper.Models;

/// <summary>View model for the edit-site page.</summary>
public class SiteEditModel
{
    public Site Site { get; set; } = new();
    public SiteInput Input { get; set; } = new();
}
