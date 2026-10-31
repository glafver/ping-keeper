using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PingKeeper.Models;

namespace PingKeeper.Controllers;

/// <summary>
/// Single-user cookie login. Credentials come from Auth:Username / Auth:Password
/// (defaults: admin / demo — change Auth:Password in production).
/// </summary>
[AllowAnonymous]
public class AuthController : Controller
{
    private readonly string _username;
    private readonly string _password;

    public AuthController(IConfiguration configuration)
    {
        _username = configuration["Auth:Username"] ?? "admin";
        _password = configuration["Auth:Password"] ?? "demo";
    }

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        SetDemoHint();
        return View(new LoginModel());
    }

    [HttpPost("/login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        if (CredentialsMatch(model.Username, model.Password))
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, _username) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/");
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        ViewBag.ReturnUrl = returnUrl;
        SetDemoHint();
        return View(model);
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }

    private bool CredentialsMatch(string username, string password)
    {
        return FixedTimeEquals(username, _username) && FixedTimeEquals(password, _password);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bytesB = Encoding.UTF8.GetBytes(b ?? string.Empty);
        return bytesA.Length == bytesB.Length
            && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private void SetDemoHint()
    {
        if (string.Equals(_password, "demo", StringComparison.Ordinal))
        {
            ViewBag.DemoHint = "Default login: admin / demo — set Auth:Password in config to change it.";
        }
    }
}
