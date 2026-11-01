namespace PingKeeper.Models;

/// <summary>
/// Default sites written to Firestore on first run, so the dashboard isn't empty.
/// These can be edited or removed later from the dashboard (behind login).
/// </summary>
public static class SeedData
{
    public static readonly Site[] Sites = new[]
    {
        new Site
        {
            Name = "WashZone",
            Url = "https://washzone-364590748641.europe-west1.run.app",
            HealthUrl = "https://washzone-364590748641.europe-west1.run.app/health",
        },
        new Site
        {
            Name = "SKILLUP",
            Url = "https://skillup-364590748641.europe-west1.run.app",
            HealthUrl = "https://skillup-364590748641.europe-west1.run.app/api/health",
        },
        new Site
        {
            Name = "Drone Delight",
            Url = "https://drone-delight-364590748641.europe-west1.run.app",
            ApiUrl = "https://drone-delight-364590748641.europe-west1.run.app/api/ping",
            HealthUrl = "https://drone-delight-364590748641.europe-west1.run.app/api/products",
        },
        new Site { Name = "Re-Yacht", Url = "https://battle-of-yachts-364590748641.europe-west1.run.app" },
        new Site { Name = "Tasty Malmö", Url = "https://restaurants-map-364590748641.europe-west1.run.app" },
        new Site { Name = "Sweet Shop", Url = "https://sweet-shop-glafver.netlify.app" },
        new Site { Name = "Photo Portfolio", Url = "https://photo.glafira.se" },
        new Site { Name = "Portfolio", Url = "https://glafira.se" },
        new Site
        {
            Name = "Ping Keeper",
            Url = "https://ping-keeper-364590748641.europe-west1.run.app",
            Enabled = false,
        },
    };
}
