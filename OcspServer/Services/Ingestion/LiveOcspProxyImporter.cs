using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using OcspServer.Models;
using OcspServer.Services.CertificateStore;
using ModelCertStatus = OcspServer.Models.CertificateStatus;

namespace OcspServer.Services.Ingestion
{
    /// <summary>
    /// Sends individual OCSP queries to a running local OpenSSL OCSP responder
    /// and upserts the results into the certificate store.
    ///
    /// This is a delta-sync tool: you provide the list of serial numbers to query
    /// and the responder URL, and this service updates their status records.
    /// </summary>
    public class LiveOcspProxyImporter
    {
        private readonly ICertificateStoreService _store;
        private readonly HttpClient _http;
        private readonly ILogger<LiveOcspProxyImporter> _logger;

        public LiveOcspProxyImporter(
            ICertificateStoreService store,
            HttpClient http,
            ILogger<LiveOcspProxyImporter> logger)
        {
            _store = store;
            _http = http;
            _logger = logger;
        }

        /// <summary>
        /// Query the given responder URL with a raw DER OCSP request and
        /// parse the DER response to extract certificate statuses.
        /// </summary>
        /// <param name="responderUrl">Base URL of the OCSP responder (e.g. "http://127.0.0.1:2560").</param>
        /// <param name="requestDer">DER-encoded OCSPRequest bytes.</param>
        /// <param name="issuerKeyHashSha1">Issuer key hash (for store lookup).</param>
        /// <returns>ImportResult summarising what was updated.</returns>
        public async Task<ImportResult> SyncAsync(
            string responderUrl,
            byte[] requestDer,
            string issuerKeyHashSha1,
            CancellationToken ct = default)
        {
            var result = new ImportResult { Source = "ocsp-proxy" };

            try
            {
                var content = new ByteArrayContent(requestDer);
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/ocsp-request");

                var url = responderUrl.TrimEnd('/');
                var response = await _http.PostAsync(url, content, ct);
                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentType?.MediaType != "application/ocsp-response")
                {
                    result.Errors.Add("Unexpected content-type from proxy responder");
                    return result;
                }

                var responseDer = await response.Content.ReadAsByteArrayAsync(ct);
                await ParseAndUpsertResponseAsync(responseDer, issuerKeyHashSha1, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LiveOcspProxyImporter: sync failed against {Url}", responderUrl);
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task ParseAndUpsertResponseAsync(
            byte[] responseDer, string issuerKeyHashSha1, ImportResult result)
        {
            OcspResp ocspResp;
            try
            {
                ocspResp = new OcspResp(responseDer);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to parse OCSP response DER: {ex.Message}");
                return;
            }

            if (ocspResp.Status != OcspRespStatus.Successful)
            {
                result.Errors.Add($"OCSP responder returned non-successful status: {ocspResp.Status}");
                return;
            }

            BasicOcspResp basic;
            try
            {
                basic = (BasicOcspResp)ocspResp.GetResponseObject();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to parse BasicOCSPResponse: {ex.Message}");
                return;
            }

            foreach (var singleResp in basic.Responses)
            {
                var certId = singleResp.GetCertID();
                var serial = certId.SerialNumber.ToString(16).ToUpperInvariant().TrimStart('0');
                if (serial.Length == 0) serial = "0";

                var existing = await _store.FindBySerialAndIssuerKeyHashAsync(serial, issuerKeyHashSha1);

                ModelCertStatus status;
                DateTime? revDate = null;
                RevocationReason reason = RevocationReason.Unspecified;

                if (singleResp.GetCertStatus() == null)
                {
                    status = ModelCertStatus.Good;
                }
                else if (singleResp.GetCertStatus() is RevokedStatus revokedStatus)
                {
                    status = ModelCertStatus.Revoked;
                    revDate = revokedStatus.RevocationTime.ToUniversalTime();
                    if (revokedStatus.HasRevocationReason)
                        reason = (RevocationReason)revokedStatus.RevocationReason;
                }
                else
                {
                    status = ModelCertStatus.Unknown;
                }

                if (existing != null)
                {
                    if (existing.Status != status ||
                        existing.RevocationDate != revDate ||
                        existing.RevocationReason != reason)
                    {
                        await _store.UpdateStatusAsync(existing.Id, status, revDate, reason, "ocsp-proxy");
                        result.Updated++;
                    }
                    else
                    {
                        result.Unchanged++;
                    }
                }
                else
                {
                    await _store.UpsertAsync(new CertificateRecord
                    {
                        SerialNumber = serial,
                        IssuerKeyHashSha1 = issuerKeyHashSha1,
                        IssuerKeyHashSha256 = string.Empty,
                        IssuerNameHashSha1 = string.Empty,
                        IssuerDN = string.Empty,
                        SubjectDN = string.Empty,
                        Status = status,
                        RevocationDate = revDate,
                        RevocationReason = reason,
                        NotBefore = DateTime.MinValue,
                        NotAfter = singleResp.NextUpdate?.ToUniversalTime() ?? DateTime.MaxValue,
                        ImportSource = "ocsp-proxy",
                        LastUpdated = DateTime.UtcNow
                    });
                    result.Added++;
                }
            }
        }
    }
}
