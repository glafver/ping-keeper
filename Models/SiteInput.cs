using System.ComponentModel.DataAnnotations;

namespace PingKeeper.Models;

/// <summary>Form model for adding/editing a monitored site.</summary>
public class SiteInput
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500), Url]
    public string Url { get; set; } = string.Empty;

    [StringLength(500), Url]
    public string? ApiUrl { get; set; }

    [StringLength(500), Url]
    public string? HealthUrl { get; set; }

    [Range(1, 10080)]
    public int IntervalValue { get; set; } = 1;

    public string IntervalUnit { get; set; } = "days";

    public bool CheckFrontend { get; set; } = true;
    public bool CheckBackend { get; set; } = true;
    public bool CheckDatabase { get; set; } = true;
}
