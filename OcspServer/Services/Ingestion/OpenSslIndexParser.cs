using OcspServer.Models;

namespace OcspServer.Services.Ingestion
{
    /// <summary>
    /// Parses an OpenSSL CA database (index.txt) file into <see cref="CertificateRecord"/> objects.
    ///
    /// Format of each tab-delimited line:
    ///   Flag  ExpDate  RevDate  Serial  Filename  Subject
    ///
    /// Flag:    V = valid, R = revoked, E = expired
    /// ExpDate: YYMMDDHHMMSSZ
    /// RevDate: YYMMDDHHMMSSZ[,reason]  (only present for R lines)
    /// Serial:  hex string (upper or lower case)
    /// Filename: usually "unknown" in OpenSSL CA databases
    /// Subject: /C=.../O=.../CN=...
    ///
    /// Reason codes follow the OpenSSL convention:
    ///   keyCompromise, CACompromise, affiliationChanged, superseded,
    ///   cessationOfOperation, certificateHold, removeFromCRL,
    ///   privilegeWithdrawn, AACompromise
    /// </summary>
    public class OpenSslIndexParser
    {
        // Placeholder issuer hash values; real values require the CA certificate
        // which may not be available during import. The calling code should
        // populate IssuerKeyHashSha1 / IssuerKeyHashSha256 from the CA cert if available.
        private const string UnknownHash = "0000000000000000000000000000000000000000";

        private readonly string _issuerDN;
        private readonly string _issuerKeyHashSha1;
        private readonly string _issuerKeyHashSha256;
        private readonly string _issuerNameHashSha1;
        private readonly ILogger<OpenSslIndexParser> _logger;

        public OpenSslIndexParser(
            ILogger<OpenSslIndexParser> logger,
            string issuerDN = "",
            string issuerKeyHashSha1 = "",
            string issuerKeyHashSha256 = "",
            string issuerNameHashSha1 = "")
        {
            _logger = logger;
            _issuerDN = issuerDN;
            _issuerKeyHashSha1 = string.IsNullOrEmpty(issuerKeyHashSha1) ? UnknownHash : issuerKeyHashSha1;
            _issuerKeyHashSha256 = string.IsNullOrEmpty(issuerKeyHashSha256) ? UnknownHash : issuerKeyHashSha256;
            _issuerNameHashSha1 = string.IsNullOrEmpty(issuerNameHashSha1) ? UnknownHash : issuerNameHashSha1;
        }

        /// <summary>Parse all lines from an index.txt stream.</summary>
        public IEnumerable<(CertificateRecord Record, string? Error)> Parse(Stream stream)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            int lineNum = 0;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    continue;

                var result = ParseLine(line, lineNum);
                yield return result;
            }
        }

        /// <summary>Parse all lines from an index.txt string.</summary>
        public IEnumerable<(CertificateRecord Record, string? Error)> ParseText(string text)
        {
            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
            return Parse(ms).ToList();
        }

        // ── Private helpers ────────────────────────────────────────────────

        private (CertificateRecord Record, string? Error) ParseLine(string line, int lineNum)
        {
            var parts = line.Split('\t');
            if (parts.Length < 6)
            {
                var msg = $"Line {lineNum}: expected 6 tab-delimited fields, got {parts.Length}";
                _logger.LogWarning("{Msg}", msg);
                return (new CertificateRecord(), msg);
            }

            var flag = parts[0].Trim();
            var expDateStr = parts[1].Trim();
            var revDateStr = parts[2].Trim();
            var serial = parts[3].Trim().ToUpperInvariant().TrimStart('0');
            // parts[4] is filename – not used
            var subject = parts[5].Trim();

            if (serial.Length == 0) serial = "0";

            CertificateStatus status;
            DateTime? revocationDate = null;
            RevocationReason reason = RevocationReason.Unspecified;

            switch (flag.ToUpperInvariant())
            {
                case "V":
                    status = CertificateStatus.Good;
                    break;
                case "E":
                    // Treat expired entries as Good (status reflects revocation, not expiry)
                    status = CertificateStatus.Good;
                    break;
                case "R":
                    status = CertificateStatus.Revoked;
                    if (!string.IsNullOrEmpty(revDateStr))
                    {
                        var revParts = revDateStr.Split(',');
                        revocationDate = ParseOpenSslDate(revParts[0]);
                        if (revParts.Length > 1)
                            reason = ParseReason(revParts[1]);
                    }
                    break;
                default:
                    var msg = $"Line {lineNum}: unknown status flag '{flag}'";
                    _logger.LogWarning("{Msg}", msg);
                    return (new CertificateRecord(), msg);
            }

            var notAfter = ParseOpenSslDate(expDateStr) ?? DateTime.UtcNow;

            var record = new CertificateRecord
            {
                SerialNumber = serial,
                IssuerKeyHashSha1 = _issuerKeyHashSha1,
                IssuerKeyHashSha256 = _issuerKeyHashSha256,
                IssuerNameHashSha1 = _issuerNameHashSha1,
                IssuerDN = _issuerDN,
                SubjectDN = subject,
                Status = status,
                RevocationDate = revocationDate,
                RevocationReason = reason,
                NotBefore = DateTime.MinValue,   // not available in index.txt
                NotAfter = notAfter,
                ImportSource = "index.txt",
                LastUpdated = DateTime.UtcNow
            };

            return (record, null);
        }

        /// <summary>Parse OpenSSL's YYMMDDHHMMSSZ date format.</summary>
        internal static DateTime? ParseOpenSslDate(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            // Remove trailing 'Z'
            s = s.TrimEnd('Z');
            if (s.Length == 12 &&
                int.TryParse(s[..2], out int yy) &&
                int.TryParse(s[2..4], out int mm) &&
                int.TryParse(s[4..6], out int dd) &&
                int.TryParse(s[6..8], out int hh) &&
                int.TryParse(s[8..10], out int min) &&
                int.TryParse(s[10..12], out int sec))
            {
                int year = yy >= 50 ? 1900 + yy : 2000 + yy;
                try
                {
                    return new DateTime(year, mm, dd, hh, min, sec, DateTimeKind.Utc);
                }
                catch { }
            }
            return null;
        }

        internal static RevocationReason ParseReason(string reason)
        {
            return reason.ToLowerInvariant() switch
            {
                "keycompromise" => RevocationReason.KeyCompromise,
                "cacompromise" => RevocationReason.CACompromise,
                "affiliationchanged" => RevocationReason.AffiliationChanged,
                "superseded" => RevocationReason.Superseded,
                "cessationofoperation" => RevocationReason.CessationOfOperation,
                "certificatehold" => RevocationReason.CertificateHold,
                "removefromcrl" => RevocationReason.RemoveFromCRL,
                "privilegewithdrawn" => RevocationReason.PrivilegeWithdrawn,
                "aacompromise" => RevocationReason.AACompromise,
                _ => RevocationReason.Unspecified
            };
        }
    }
}
