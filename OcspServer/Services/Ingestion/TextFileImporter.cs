using OcspServer.Models;

namespace OcspServer.Services.Ingestion
{
    /// <summary>
    /// Parses a simple text file where each non-blank, non-comment line is:
    ///   SERIAL  STATUS  [REVDATE]
    ///
    /// SERIAL  – hex serial number
    /// STATUS  – one of: good, valid, revoked, unknown
    /// REVDATE – optional ISO-8601 revocation date (ignored when status is not revoked)
    ///
    /// Example:
    ///   # My cert list
    ///   0A1B2C  good
    ///   0D3E4F  revoked  2024-06-01T00:00:00Z
    /// </summary>
    public class TextFileImporter
    {
        private readonly string _issuerDN;
        private readonly string _issuerKeyHashSha1;
        private readonly string _issuerKeyHashSha256;
        private readonly ILogger<TextFileImporter> _logger;

        private const string UnknownHash = "0000000000000000000000000000000000000000";

        public TextFileImporter(
            ILogger<TextFileImporter> logger,
            string issuerDN = "",
            string issuerKeyHashSha1 = "",
            string issuerKeyHashSha256 = "")
        {
            _logger = logger;
            _issuerDN = issuerDN;
            _issuerKeyHashSha1 = string.IsNullOrEmpty(issuerKeyHashSha1) ? UnknownHash : issuerKeyHashSha1;
            _issuerKeyHashSha256 = string.IsNullOrEmpty(issuerKeyHashSha256) ? UnknownHash : issuerKeyHashSha256;
        }

        public IEnumerable<(CertificateRecord Record, string? Error)> Parse(Stream stream)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            int lineNum = 0;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    var msg = $"Line {lineNum}: expected at least SERIAL STATUS";
                    _logger.LogWarning("{Msg}", msg);
                    yield return (new CertificateRecord(), msg);
                    continue;
                }

                var serial = parts[0].ToUpperInvariant().TrimStart('0');
                if (serial.Length == 0) serial = "0";

                var statusStr = parts[1].ToLowerInvariant();
                CertificateStatus status = statusStr switch
                {
                    "good" or "valid" or "v" => CertificateStatus.Good,
                    "revoked" or "r" => CertificateStatus.Revoked,
                    "unknown" or "u" => CertificateStatus.Unknown,
                    _ => CertificateStatus.Unknown
                };

                DateTime? revDate = null;
                if (status == CertificateStatus.Revoked && parts.Length >= 3)
                {
                    if (DateTime.TryParse(parts[2], out var d))
                        revDate = d.ToUniversalTime();
                }

                yield return (new CertificateRecord
                {
                    SerialNumber = serial,
                    IssuerKeyHashSha1 = _issuerKeyHashSha1,
                    IssuerKeyHashSha256 = _issuerKeyHashSha256,
                    IssuerNameHashSha1 = UnknownHash,
                    IssuerDN = _issuerDN,
                    SubjectDN = string.Empty,
                    Status = status,
                    RevocationDate = revDate,
                    RevocationReason = status == CertificateStatus.Revoked
                        ? RevocationReason.Unspecified : RevocationReason.Unspecified,
                    NotBefore = DateTime.MinValue,
                    NotAfter = DateTime.MaxValue,
                    ImportSource = "textfile",
                    LastUpdated = DateTime.UtcNow
                }, null);
            }
        }
    }
}
