using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Security;
using OcspServer.Models;
using OcspServer.Models.Settings;
using BcCertStatus = Org.BouncyCastle.Ocsp.CertificateStatus;

namespace OcspServer.Services.Ocsp
{
    /// <summary>
    /// Builds a signed, DER-encoded OCSPResponse wrapping a BasicOCSPResponse
    /// (RFC 6960 §4.2.1).
    ///
    /// BouncyCastle 2.x certificate status conventions:
    ///   CertificateStatus.Good  → good
    ///   RevokedStatus           → revoked
    ///   UnknownStatus           → unknown
    /// </summary>
    public class OcspResponseBuilder
    {
        private readonly OcspServerSettings _settings;
        private readonly OcspSigningService _signer;
        private readonly ILogger<OcspResponseBuilder> _logger;

        public OcspResponseBuilder(
            OcspServerSettings settings,
            OcspSigningService signer,
            ILogger<OcspResponseBuilder> logger)
        {
            _settings = settings;
            _signer = signer;
            _logger = logger;
        }

        /// <summary>
        /// Build a successful (responseStatus = successful) signed OCSP response.
        /// </summary>
        /// <param name="certIds">Parsed cert identifiers from the request.</param>
        /// <param name="statusMap">Map from serial hex → CertificateRecord (null = unknown).</param>
        /// <param name="requestNonce">Nonce bytes from the request (null if absent).</param>
        /// <returns>DER-encoded OCSPResponse bytes.</returns>
        public byte[] BuildSuccessResponse(
            IReadOnlyList<ParsedCertId> certIds,
            IReadOnlyDictionary<string, Models.CertificateRecord?> statusMap,
            byte[]? requestNonce)
        {
            var (sigAlgName, privateKey, responderPublicKey, chain) = _signer.GetSigningMaterial();

            var basicGenerator = new BasicOcspRespGenerator(responderPublicKey);

            var now = DateTime.UtcNow;
            DateTime nextUpdate = now.AddHours(_settings.NextUpdateHours);

            foreach (var certId in certIds)
            {
                statusMap.TryGetValue(certId.SerialHex, out var record);
                BcCertStatus bcStatus = BuildCertStatus(record);

                basicGenerator.AddResponse(
                    certId.RawCertId,
                    bcStatus,
                    now,
                    nextUpdate,
                    null);   // per-response single extensions
            }

            // Echo nonce in the BasicOCSPResponse extensions if present in request (RFC 8954)
            if (requestNonce != null && _settings.AllowNonce)
            {
                var nonceExtensions = new X509Extensions(
                    new Dictionary<DerObjectIdentifier, X509Extension>
                    {
                        {
                            OcspObjectIdentifiers.PkixOcspNonce,
                            new X509Extension(false, new DerOctetString(requestNonce))
                        }
                    });
                basicGenerator.SetResponseExtensions(nonceExtensions);
            }

            var basicResp = basicGenerator.Generate(sigAlgName, privateKey, chain, now,
                new SecureRandom());

            var ocspRespGen = new OCSPRespGenerator();
            var ocspResp = ocspRespGen.Generate(OCSPRespGenerator.Successful, basicResp);
            return ocspResp.GetEncoded();
        }

        /// <summary>
        /// Build a DER-encoded error response.
        /// Error responses carry no signature (RFC 6960 §4.2.2).
        /// </summary>
        public static byte[] BuildErrorResponse(int responseStatus)
        {
            var gen = new OCSPRespGenerator();
            var resp = gen.Generate(responseStatus, null);
            return resp.GetEncoded();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Map a <see cref="Models.CertificateRecord"/> to a BouncyCastle cert-status value.
        /// </summary>
        private static BcCertStatus BuildCertStatus(Models.CertificateRecord? record)
        {
            if (record == null)
                return new UnknownStatus();

            return record.Status switch
            {
                Models.CertificateStatus.Good => BcCertStatus.Good,
                Models.CertificateStatus.Revoked => new RevokedStatus(
                    record.RevocationDate ?? DateTime.UtcNow,
                    (int)record.RevocationReason),
                _ => new UnknownStatus()
            };
        }
    }
}
