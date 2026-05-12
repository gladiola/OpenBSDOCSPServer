using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OcspServer.Models.Settings;
using OcspServer.Resources;

namespace OcspServer.Controllers
{
    /// <summary>
    /// Handles admin login / logout.
    ///
    /// When <see cref="FeatureFlags.EnableEntraIdAuth"/> is true:
    ///   – GET /account/login   → challenges Entra ID via OIDC (no local form).
    ///   – POST /account/logout → signs out of both OIDC and the OIDC cookie.
    ///
    /// When <see cref="FeatureFlags.EnableAdminAuth"/> is true (local auth):
    ///   – GET  /account/login  → renders the local credentials form.
    ///   – POST /account/login  → validates PBKDF2 hash and issues AdminCookie.
    ///   – POST /account/logout → signs out of AdminCookie.
    ///
    /// Hash format for local auth: "iterations:base64salt:base64hash"
    /// Generate via: dotnet run --project OcspServer -- hash-password
    /// </summary>
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly AdminAuthSettings _authSettings;
        private readonly ILogger<AccountController> _logger;
        private readonly FeatureFlags _flags;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(
            AdminAuthSettings authSettings,
            IOptionsMonitor<FeatureFlags> flagsMonitor,
            IStringLocalizer<SharedResource> localizer,
            ILogger<AccountController> logger)
        {
            _authSettings = authSettings;
            _flags = flagsMonitor.CurrentValue;
            _localizer = localizer;
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAdminDashboard(returnUrl);

            // When EntraID auth is active, challenge with OIDC immediately.
            if (_flags.EnableEntraIdAuth)
            {
                var redirectUrl = Url.Action("Dashboard", "Admin") ?? "/admin";
                return Challenge(
                    new AuthenticationProperties { RedirectUri = redirectUrl },
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username, string password, string? returnUrl = null)
        {
            // This POST action is only reached in local-auth mode.
            if (_flags.EnableEntraIdAuth)
                return RedirectToAdminDashboard(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", _localizer["ErrorUsernamePasswordRequired"]);
                return View();
            }

            if (!ValidateCredentials(username, password))
            {
                _logger.LogWarning("Failed admin login attempt for user '{User}' from {IP}",
                    username, HttpContext.Connection.RemoteIpAddress);
                // Constant-time delay to mitigate brute-force timing side-channels
                await Task.Delay(300);
                ModelState.AddModelError("", _localizer["ErrorInvalidCredentials"]);
                return View();
            }

            _logger.LogInformation("Admin login succeeded for '{User}'", username);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookie", principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_authSettings.SessionTimeoutMinutes)
                });

            return RedirectToAdminDashboard(returnUrl);
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (_flags.EnableEntraIdAuth)
            {
                // Sign out of both the OIDC session and the OIDC cookie.
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(
                    Microsoft.AspNetCore.Authentication.Cookies
                        .CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            await HttpContext.SignOutAsync("AdminCookie");
            return RedirectToAction(nameof(Login));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private bool ValidateCredentials(string username, string password)
        {
            if (!string.Equals(username, _authSettings.AdminUsername,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(_authSettings.AdminPasswordHash))
            {
                _logger.LogWarning("AdminPasswordHash is not configured – login disabled");
                return false;
            }

            return VerifyPbkdf2(password, _authSettings.AdminPasswordHash);
        }

        /// <summary>
        /// Verify a plaintext password against a stored PBKDF2 hash.
        /// Format: "iterations:base64salt:base64hash"
        /// </summary>
        private static bool VerifyPbkdf2(string password, string stored)
        {
            try
            {
                var parts = stored.Split(':');
                if (parts.Length != 3) return false;

                int iterations = int.Parse(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expectedHash = Convert.FromBase64String(parts[2]);

                var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, iterations, HashAlgorithmName.SHA256);
                byte[] actualHash = pbkdf2.GetBytes(32);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate a PBKDF2 hash for a given password (utility for setup).
        /// Returns "iterations:base64salt:base64hash".
        /// </summary>
        public static string HashPassword(string password, int iterations = 310_000)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);
            return $"{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        private IActionResult RedirectToAdminDashboard(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Dashboard", "Admin");
        }
    }
}
