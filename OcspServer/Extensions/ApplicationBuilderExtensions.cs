namespace OcspServer.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Applies standard HTTP security headers to every response that is NOT
        /// an OCSP endpoint response.
        ///
        /// OCSP endpoints (/ocsp/**) MUST return Content-Type: application/ocsp-response
        /// with a binary DER body.  Injecting an HTML-oriented Content-Type header
        /// or a Cache-Control: no-store header onto those responses would violate
        /// RFC 6960 §4.2.1 and RFC 5019 §2.2.  Therefore this middleware skips
        /// all header injection for any request path that starts with /ocsp.
        ///
        /// All other paths (admin UI, static assets, etc.) receive the full set of
        /// hardening headers.
        /// </summary>
        public static IApplicationBuilder UseStandardSecurityHeaders(
            this IApplicationBuilder app, ILogger logger, bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Standard security headers are DISABLED");
                return app;
            }

            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                bool isOcspEndpoint = path.StartsWith("/ocsp", StringComparison.OrdinalIgnoreCase);

                if (!isOcspEndpoint)
                {
                    // Frame / click-jacking protection
                    context.Response.Headers.Append("X-Frame-Options", "DENY");

                    // XSS filter (legacy browsers)
                    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

                    // MIME sniffing prevention
                    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

                    // Transport security (1 year)
                    context.Response.Headers.Append(
                        "Strict-Transport-Security",
                        "max-age=31536000; includeSubDomains");

                    // Cross-origin isolation
                    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                    context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-site");

                    // Permissions policy – disable sensors and tracking APIs
                    context.Response.Headers.Append(
                        "Permissions-Policy",
                        "geolocation=(), camera=(), microphone=(), interest-cohort=()");

                    // Suppress server version banners
                    context.Response.Headers.Remove("Server");
                    context.Response.Headers.Append("Server", "ocsp-responder");
                    context.Response.Headers.Remove("X-Powered-By");
                    context.Response.Headers.Remove("X-AspNetMvc-Version");

                    // Prevent caching of admin HTML pages
                    context.Response.Headers.Append(
                        "Cache-Control", "no-cache, no-store, must-revalidate");
                    context.Response.Headers.Append("Pragma", "no-cache");
                    context.Response.Headers.Append("Expires", "0");
                }
                else
                {
                    // For OCSP endpoints: only strip server-identification headers.
                    // Content-Type, Cache-Control, and all other headers are set
                    // exclusively by OcspController to comply with RFC 6960 and RFC 5019.
                    context.Response.Headers.Remove("Server");
                    context.Response.Headers.Remove("X-Powered-By");
                    context.Response.Headers.Remove("X-AspNetMvc-Version");
                }

                await next.Invoke();
            });

            logger.LogInformation("Security headers middleware configured (OCSP paths exempt from Content-Type / Cache-Control override)");
            return app;
        }
    }
}
