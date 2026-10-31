using System.Threading.RateLimiting;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PingKeeper.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor views. Auto-validate antiforgery tokens on all unsafe methods (CSRF protection).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// --- Authentication: single-user cookie login (personal tool) ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.Cookie.Name = "pingkeeper_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Everything requires auth by default; /login and /ping opt out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Rate limiting to slow down brute-force login attempts.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
            }));
});

// --- Persistence: Firestore (prod) or in-memory demo (local) ---
var useDemo = builder.Configuration.GetValue<bool>("Demo:Enabled")
    || string.Equals(
        Environment.GetEnvironmentVariable("PINGKEEPER_DEMO"),
        "1",
        StringComparison.OrdinalIgnoreCase);

if (useDemo)
{
    builder.Services.AddSingleton<IMonitorStore, DemoStore>();
}
else
{
    // Project id comes from config (Firestore:ProjectId), then the
    // GOOGLE_CLOUD_PROJECT env var (set automatically on Cloud Run).
    var projectId = builder.Configuration["Firestore:ProjectId"]?.Trim();
    if (string.IsNullOrEmpty(projectId))
    {
        projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
    }

    if (string.IsNullOrEmpty(projectId))
    {
        throw new InvalidOperationException(
            "Firestore project id is not configured. Set 'Firestore:ProjectId' in config " +
            "or the GOOGLE_CLOUD_PROJECT environment variable.");
    }

    // FirestoreDb is thread-safe; register as a singleton.
    // Uses Application Default Credentials (automatic on Cloud Run; locally set
    // GOOGLE_APPLICATION_CREDENTIALS to a service-account JSON key).
    builder.Services.AddSingleton(FirestoreDb.Create(projectId));
    builder.Services.AddSingleton<IMonitorStore, FirestoreService>();
}

// --- App services ---
builder.Services.AddSingleton<PingService>();
builder.Services.AddSingleton<DashboardService>();

// HttpClient used to probe the monitored sites.
builder.Services.AddHttpClient("ping", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PingKeeper/1.0");
});

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
