using Microsoft.AspNetCore.Mvc;
using OcspServer.Models;
using OcspServer.Models.Settings;
using OcspServer.Services.Ocsp;
using OcspServer.Services.CertificateStore;

namespace OcspServer.Controllers
{
    /// <summary>
    /// RFC 6960 / RFC 5019 OCSP responder endpoint.
    ///
    /// POST /ocsp          — body: DER-encoded OCSPRequest; RFC 6960 §4.1
    /// GET  /ocsp/{b64}    — path param: base64url-encoded OCSPRequest; RFC 5019
    ///
    /// This endpoint is intentionally anonymous and read-only.
    /// Responses are DER-encoded OCSPResponse with Content-Type: application/ocsp-response.
    /// Cache-Control is set per RFC 5019 §2.2 based on the NextUpdate window.
    /// </summary>
    [Route("ocsp")]
    public class OcspController : ControllerBase
    {
        private readonly OcspRequestParser _requestParser;
        private readonly CertIdResolver _resolver;
        private readonly OcspResponseBuilder _responseBuilder;
        private readonly OcspServerSettings _settings;
        private readonly ILogger<OcspController> _logger;

        private const string OcspRequestContentType = "application/ocsp-request";
        private const string OcspResponseContentType = "application/ocsp-response";

        public OcspController(
            OcspRequestParser requestParser,
            CertIdResolver resolver,
            OcspResponseBuilder responseBuilder,
            OcspServerSettings settings,
            ILogger<OcspController> logger)
        {
            _requestParser = requestParser;
            _resolver = resolver;
            _responseBuilder = responseBuilder;
            _settings = settings;
            _logger = logger;
        }

        // ── POST /ocsp ──────────────────────────────────────────────────────

        /// <summary>
        /// Handle an OCSP request sent as a POST with body application/ocsp-request.
        /// RFC 6960 §4.1.
        /// </summary>
        [HttpPost]
        [Consumes(OcspRequestContentType)]
        public async Task<IActionResult> Post()
        {
            if (!_settings.AllowPostRequests)
                return OcspError(OcspResponseStatus.MalformedRequest);

            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            var der = ms.ToArray();

            return await HandleRequestDer(der);
        }

        // ── GET /ocsp/{base64url} ────────────────────────────────────────────

        /// <summary>
        /// Handle an OCSP request sent as a GET with base64url-encoded DER in the path.
        /// RFC 5019 §2.1.
        /// </summary>
        [HttpGet("{base64Url}")]
        public async Task<IActionResult> Get(string base64Url)
        {
            if (!_settings.AllowGetRequests)
                return OcspError(OcspResponseStatus.MalformedRequest);

            byte[] der;
            try
            {
                // base64url uses - and _ instead of + and /; restore standard base64
                var b64 = base64Url
                    .Replace('-', '+')
                    .Replace('_', '/')
                    .Replace(' ', '+'); // some clients encode spaces instead of +
                // Pad to multiple of 4
                int pad = b64.Length % 4;
                if (pad == 2) b64 += "==";
                else if (pad == 3) b64 += "=";

                der = Convert.FromBase64String(b64);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning("GET OCSP: invalid base64url: {Error}", ex.Message);
                return OcspError(OcspResponseStatus.MalformedRequest);
            }

            return await HandleRequestDer(der);
        }

        // ── Private logic ────────────────────────────────────────────────────

        private async Task<IActionResult> HandleRequestDer(byte[] der)
        {
            // 1. Parse
            var parsed = _requestParser.TryParse(der, out var parseError);
            if (parsed == null)
            {
                _logger.LogWarning("OCSP parse error: {Error}", parseError);
                return OcspError(OcspResponseStatus.MalformedRequest);
            }

            // 2. Resolve certificate statuses
            Dictionary<string, CertificateRecord?> statusMap;
            try
            {
                statusMap = await _resolver.ResolveManyAsync(parsed.CertIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCSP: certificate store lookup failed");
                return OcspError(OcspResponseStatus.InternalError);
            }

            // 3. Build signed response
            byte[] responseDer;
            try
            {
                responseDer = _responseBuilder.BuildSuccessResponse(
                    parsed.CertIds,
                    statusMap,
                    parsed.Nonce);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCSP: failed to build response");
                return OcspError(OcspResponseStatus.InternalError);
            }

            // 4. Set RFC 5019 Cache-Control headers on the HTTP response.
            //    max-age = NextUpdateHours * 3600 (conservative: use half the window)
            int maxAgeSeconds = (_settings.NextUpdateHours * 3600) / 2;
            Response.Headers.Append("Cache-Control", $"max-age={maxAgeSeconds}, public, no-transform");

            return OcspResponse(responseDer);
        }

        /// <summary>
        /// Return a successful DER OCSP response with the correct media type.
        /// </summary>
        private FileContentResult OcspResponse(byte[] der)
            => File(der, OcspResponseContentType);

        /// <summary>
        /// Return a DER-encoded OCSP error response (no signature, RFC 6960 §4.2.2).
        /// </summary>
        private FileContentResult OcspError(int responseStatus)
        {
            var der = OcspResponseBuilder.BuildErrorResponse(responseStatus);
            return File(der, OcspResponseContentType);
        }
    }

    /// <summary>
    /// OCSP responseStatus integer values (RFC 6960 §4.2.1).
    /// These match the constants in Org.BouncyCastle.Ocsp.OcspRespGenerator.
    /// </summary>
    public static class OcspResponseStatus
    {
        public const int Successful = 0;
        public const int MalformedRequest = 1;
        public const int InternalError = 2;
        public const int TryLater = 3;
        public const int SigRequired = 5;
        public const int Unauthorized = 6;
    }
}
