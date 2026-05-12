using OcspServer.Extensions;
using OcspServer.Models.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using OcspServer.Services.CertificateStore;
using System.Globalization;

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

            // ── Admin authentication (local cookie or Entra ID) ────────────────
            if (flags.EnableEntraIdAuth)
            {
                builder.Services.AddEntraIdAdminAuthentication(configuration, logger);
            }
            else if (flags.EnableAdminAuth)
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
            builder.Services.AddIngestionServices(flags.EnableIndexTxtWatch);

            // ── Localization + MVC ────────────────────────────────────────
            var supportedCultures = new[]
            {
                "en-US", "de-DE", "es-ES", "fr-FR", "pt-PT", "it-IT", "zh-HK", "ko-KR", "hi-IN", "ru-RU",
                "ar-SA", "sw-KE", "ja-JP", "ht-HT", "haw-US", "sm-WS", "mi-NZ", "af-ZA", "nl-NL", "ha-NG",
                "am-ET", "yo-NG", "bn-BD", "zh-CN", "et-EE", "fi-FI", "sv-SE", "nb-NO", "uk-UA", "th-TH",
                "id-ID", "tl-PH", "ms-MY", "jv-ID", "el-GR", "la-VA", "he-IL", "ga-IE"
            }.Select(c => new CultureInfo(c)).ToArray();

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.ApplyCurrentCultureToResponseHeaders = true;
            });

            builder.Services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();
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
            var requestLocalizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(requestLocalizationOptions);
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
