using Microsoft.AspNetCore.Authentication.Certificate;
using OcspServer.Models.Settings;
using OcspServer.Services.CertificateStore;
using OcspServer.Services.Ingestion;
using OcspServer.Services.Ocsp;
using System.Security.Cryptography.X509Certificates;

namespace OcspServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Load FeatureFlags from configuration into DI.</summary>
        public static IServiceCollection AddFeatureFlags(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FeatureFlags>(configuration.GetSection("FeatureFlags"));
            return services;
        }

        /// <summary>Configure session with a configurable timeout.</summary>
        public static IServiceCollection AddSessionConfiguration(
            this IServiceCollection services, ILogger logger, int timeoutMinutes = 30)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(timeoutMinutes);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });
            logger.LogInformation("Session configured with {Timeout}-minute timeout", timeoutMinutes);
            return services;
        }

        /// <summary>
        /// Register local cookie authentication for the admin UI.
        /// Credentials are validated against the PBKDF2 hash in AdminAuthSettings.
        /// </summary>
        public static IServiceCollection AddAdminAuthentication(
            this IServiceCollection services, ILogger logger)
        {
            services.AddAuthentication("AdminCookie")
                .AddCookie("AdminCookie", options =>
                {
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.AccessDeniedPath = "/account/login";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                });

            services.AddAuthorization(opts =>
            {
                opts.AddPolicy("AdminOnly", policy =>
                    policy.RequireAuthenticatedUser()
                          .AddAuthenticationSchemes("AdminCookie"));
            });

            logger.LogInformation("Admin cookie authentication configured");
            return services;
        }

        /// <summary>Optional mTLS client certificate authentication.</summary>
        public static IServiceCollection AddMtlsAuthentication(
            this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            services.AddAuthentication()
                .AddCertificate(options =>
                {
                    options.AllowedCertificateTypes = CertificateTypes.All;
                    options.RevocationMode = X509RevocationMode.NoCheck;
                    options.Events = new CertificateAuthenticationEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            logger.LogWarning("mTLS authentication failed: {Error}",
                                ctx.Exception?.Message);
                            return Task.CompletedTask;
                        },
                        OnCertificateValidated = ctx =>
                        {
                            logger.LogInformation("mTLS authenticated: {Subject}",
                                ctx.ClientCertificate.Subject);
                            return Task.CompletedTask;
                        }
                    };
                });

            logger.LogInformation("mTLS client certificate authentication configured");
            return services;
        }

        /// <summary>Register all OCSP engine services.</summary>
        public static IServiceCollection AddOcspServices(
            this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var ocspSettings = configuration.GetSection("OcspServer").Get<OcspServerSettings>()
                ?? new OcspServerSettings();
            services.AddSingleton(ocspSettings);
            services.AddSingleton<OcspSigningService>();
            services.AddSingleton<OcspRequestParser>();
            services.AddSingleton<OcspResponseBuilder>();
            services.AddSingleton<CertIdResolver>();

            logger.LogInformation("OCSP engine services registered");
            return services;
        }

        /// <summary>Register the SQLite certificate store.</summary>
        public static IServiceCollection AddCertificateStore(
            this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var ingestionSettings = configuration.GetSection("Ingestion").Get<IngestionSettings>()
                ?? new IngestionSettings();
            services.AddSingleton(ingestionSettings);

            services.AddSingleton<ICertificateStoreService>(sp =>
            {
                var storeLogger = sp.GetRequiredService<ILogger<SqliteCertificateStoreService>>();
                return new SqliteCertificateStoreService(ingestionSettings.DatabasePath, storeLogger);
            });

            logger.LogInformation("SQLite certificate store registered at {Path}",
                ingestionSettings.DatabasePath);
            return services;
        }

        /// <summary>Register ingestion services.</summary>
        public static IServiceCollection AddIngestionServices(
            this IServiceCollection services)
        {
            services.AddHttpClient<LiveOcspProxyImporter>();
            services.AddTransient<OpenSslIndexParser>();
            services.AddTransient<TextFileImporter>();
            return services;
        }
    }
}
