using OcspServer.Models;
using OcspServer.Services.CertificateStore;

namespace OcspServer.Services.Ocsp
{
    /// <summary>
    /// Resolves a <see cref="ParsedCertId"/> to a <see cref="CertificateRecord"/>
    /// by looking up the (serial, issuer key hash) pair in the certificate store.
    ///
    /// Both SHA-1 and SHA-256 issuer key hashes are tried; SHA-1 was the original
    /// algorithm specified in RFC 2560 and is still the most common in practice,
    /// but SHA-256 support is required for modern clients.
    /// </summary>
    public class CertIdResolver
    {
        private readonly ICertificateStoreService _store;
        private readonly ILogger<CertIdResolver> _logger;

        public CertIdResolver(ICertificateStoreService store, ILogger<CertIdResolver> logger)
        {
            _store = store;
            _logger = logger;
        }

        /// <summary>
        /// Resolve a single cert ID. Returns null if not found (status = unknown).
        /// </summary>
        public async Task<CertificateRecord?> ResolveAsync(ParsedCertId certId)
        {
            var record = await _store.FindBySerialAndIssuerKeyHashAsync(
                certId.SerialHex,
                certId.IssuerKeyHashHex);

            if (record == null)
            {
                _logger.LogDebug(
                    "CertID not found: serial={Serial} issuerKeyHash={Hash}",
                    certId.SerialHex, certId.IssuerKeyHashHex);
            }

            return record;
        }

        /// <summary>
        /// Resolve all cert IDs in a request batch.
        /// Returns a dictionary keyed by serial hex; unknown certs map to null.
        /// </summary>
        public async Task<Dictionary<string, CertificateRecord?>> ResolveManyAsync(
            IReadOnlyList<ParsedCertId> certIds)
        {
            var result = new Dictionary<string, CertificateRecord?>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var id in certIds)
            {
                if (!result.ContainsKey(id.SerialHex))
                    result[id.SerialHex] = await ResolveAsync(id);
            }

            return result;
        }
    }
}
