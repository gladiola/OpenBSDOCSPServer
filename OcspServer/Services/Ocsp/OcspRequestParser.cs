using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using OcspServer.Models.Settings;

namespace OcspServer.Services.Ocsp
{
    /// <summary>
    /// Decodes a DER-encoded OCSPRequest (RFC 6960 §4.1.1) using BouncyCastle.
    /// </summary>
    public class OcspRequestParser
    {
        private readonly OcspServerSettings _settings;
        private readonly ILogger<OcspRequestParser> _logger;

        public OcspRequestParser(OcspServerSettings settings, ILogger<OcspRequestParser> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Parse DER bytes into a <see cref="ParsedOcspRequest"/>.
        /// Returns null and sets <paramref name="error"/> if parsing fails.
        /// </summary>
        public ParsedOcspRequest? TryParse(byte[] der, out string? error)
        {
            error = null;
            OcspReq req;
            try
            {
                req = new OcspReq(der);
            }
            catch (Exception ex)
            {
                error = $"Malformed OCSPRequest: {ex.Message}";
                _logger.LogWarning("Failed to parse OCSPRequest: {Error}", error);
                return null;
            }

            // Extract nonce (OID 1.3.6.1.5.5.7.48.1.2)
            byte[]? nonce = null;
            var exts = req.RequestExtensions;
            if (exts != null)
            {
                var nonceExt = exts.GetExtension(OcspObjectIdentifiers.PkixOcspNonce);
                if (nonceExt != null)
                {
                    // The nonce extension value is an OCTET STRING wrapping the nonce bytes
                    var asn1 = nonceExt.GetParsedValue();
                    nonce = asn1 is Asn1OctetString oct ? oct.GetOctets() : nonceExt.Value.GetOctets();

                    if (_settings.AllowNonce && nonce.Length > _settings.MaxNonceSizeBytes)
                    {
                        error = $"Nonce size {nonce.Length} exceeds maximum {_settings.MaxNonceSizeBytes} bytes (RFC 8954)";
                        _logger.LogWarning("{Error}", error);
                        return null;
                    }
                }
                else if (_settings.RequireNonce)
                {
                    error = "Nonce required but not present in request";
                    _logger.LogWarning("{Error}", error);
                    return null;
                }
            }
            else if (_settings.RequireNonce)
            {
                error = "Nonce required but request has no extensions";
                _logger.LogWarning("{Error}", error);
                return null;
            }

            var certIds = req.GetRequestList()
                .Select(r =>
                {
                    var certId = r.GetCertID();
                    var serial = certId.SerialNumber.ToString(16).ToUpperInvariant().TrimStart('0');
                    if (serial.Length == 0) serial = "0";
                    return new ParsedCertId
                    {
                        SerialHex = serial,
                        IssuerKeyHashHex = Convert.ToHexString(certId.GetIssuerKeyHash()).ToUpperInvariant(),
                        IssuerNameHashHex = Convert.ToHexString(certId.GetIssuerNameHash()).ToUpperInvariant(),
                        HashAlgorithmOid = certId.HashAlgOid,
                        RawCertId = certId
                    };
                })
                .ToList();

            if (certIds.Count == 0)
            {
                error = "OCSPRequest contained no certificate identifiers";
                return null;
            }

            return new ParsedOcspRequest
            {
                CertIds = certIds,
                Nonce = nonce,
                RawRequest = req
            };
        }
    }

    public class ParsedOcspRequest
    {
        public List<ParsedCertId> CertIds { get; init; } = new();
        public byte[]? Nonce { get; init; }
        public OcspReq RawRequest { get; init; } = null!;
    }

    public class ParsedCertId
    {
        /// <summary>Uppercase hex serial with leading zeros stripped.</summary>
        public string SerialHex { get; init; } = string.Empty;
        /// <summary>Uppercase hex issuer key hash.</summary>
        public string IssuerKeyHashHex { get; init; } = string.Empty;
        /// <summary>Uppercase hex issuer name hash.</summary>
        public string IssuerNameHashHex { get; init; } = string.Empty;
        public string HashAlgorithmOid { get; init; } = string.Empty;
        public CertificateID RawCertId { get; init; } = null!;
    }
}
