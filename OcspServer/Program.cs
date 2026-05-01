using OcspServer.Extensions;
using OcspServer.Models.Settings;
using OcspServer.Services.CertificateStore;

namespace OcspServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger("Program");

            logger.LogInformation("=== OCSP Server Starting ===");

            // ── Feature flags ──────────────────────────────────────────────
            builder.Services.AddFeatureFlags(configuration);
            var flags = configuration.GetSection("FeatureFlags").Get<FeatureFlags>()
                        ?? new FeatureFlags();

            // ── Admin auth settings ────────────────────────────────────────
            var adminAuthSettings = configuration.GetSection("AdminAuth").Get<AdminAuthSettings>()
                                    ?? new AdminAuthSettings();
            builder.Services.AddSingleton(adminAuthSettings);

            // ── Session ────────────────────────────────────────────────────
            if (flags.EnableSession)
                builder.Services.AddSessionConfiguration(logger, adminAuthSettings.SessionTimeoutMinutes);

            // ── Admin authentication (local cookie) ────────────────────────
            if (flags.EnableAdminAuth)
            {
                builder.Services.AddAdminAuthentication(logger);
            }
            else
            {
                // Dev mode: still register auth/authz so attribute filters resolve
                builder.Services.AddAuthentication().AddCookie("AdminCookie");
                builder.Services.AddAuthorization(opts =>
                    opts.AddPolicy("AdminOnly", p => p.RequireAssertion(_ => true)));
            }

            // ── Optional mTLS ──────────────────────────────────────────────
            if (flags.EnableMtls)
                builder.Services.AddMtlsAuthentication(configuration, logger);

            // ── OCSP engine & certificate store ────────────────────────────
            builder.Services.AddCertificateStore(configuration, logger);
            builder.Services.AddOcspServices(configuration, logger);
            builder.Services.AddIngestionServices();

            // ── MVC ────────────────────────────────────────────────────────
            builder.Services.AddControllersWithViews();
            builder.Services.AddAntiforgery();

            logger.LogInformation("=== Service configuration complete ===");

            // ── Build ──────────────────────────────────────────────────────
            var app = builder.Build();

            // ── DB initialisation ──────────────────────────────────────────
            var store = app.Services.GetRequiredService<ICertificateStoreService>();
            await store.InitializeAsync();
            logger.LogInformation("Certificate store initialised");

            // ── HTTP pipeline ──────────────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            if (flags.EnableSession)
                app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            if (flags.EnableSecurityHeaders)
                app.UseStandardSecurityHeaders(logger);

            app.MapControllerRoute(
                name: "admin",
                pattern: "admin/{action=Dashboard}/{id?}",
                defaults: new { controller = "Admin" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            logger.LogInformation("=== OCSP Server Ready ===");
            app.Run();
        }
    }
}
