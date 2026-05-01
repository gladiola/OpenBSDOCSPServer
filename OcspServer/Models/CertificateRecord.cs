namespace OcspServer.Models
{
    /// <summary>
    /// Revocation reason codes as defined in RFC 5280 §5.3.1.
    /// </summary>
    public enum RevocationReason
    {
        Unspecified = 0,
        KeyCompromise = 1,
        CACompromise = 2,
        AffiliationChanged = 3,
        Superseded = 4,
        CessationOfOperation = 5,
        CertificateHold = 6,
        RemoveFromCRL = 8,
        PrivilegeWithdrawn = 9,
        AACompromise = 10
    }

    /// <summary>
    /// Certificate status as returned in an OCSP response (RFC 6960 §2.2).
    /// </summary>
    public enum CertificateStatus
    {
        Good = 0,
        Revoked = 1,
        Unknown = 2
    }

    /// <summary>
    /// Persisted record for a certificate known to this OCSP responder.
    /// </summary>
    public class CertificateRecord
    {
        /// <summary>Surrogate primary key (SQLite ROWID).</summary>
        public long Id { get; set; }

        /// <summary>Certificate serial number as an uppercase hex string (e.g. "0A1B2C").</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>SHA-1 hash of the issuer's public key (hex), used for CertID matching.</summary>
        public string IssuerKeyHashSha1 { get; set; } = string.Empty;

        /// <summary>SHA-256 hash of the issuer's public key (hex), used for CertID matching.</summary>
        public string IssuerKeyHashSha256 { get; set; } = string.Empty;

        /// <summary>SHA-1 hash of the issuer's DN (hex), used for CertID matching.</summary>
        public string IssuerNameHashSha1 { get; set; } = string.Empty;

        /// <summary>Issuer distinguished name string.</summary>
        public string IssuerDN { get; set; } = string.Empty;

        /// <summary>Subject distinguished name string.</summary>
        public string SubjectDN { get; set; } = string.Empty;

        public CertificateStatus Status { get; set; } = CertificateStatus.Good;

        public DateTime? RevocationDate { get; set; }

        public RevocationReason RevocationReason { get; set; } = RevocationReason.Unspecified;

        public DateTime NotBefore { get; set; }

        public DateTime NotAfter { get; set; }

        /// <summary>Which import mechanism last updated this record (e.g. "index.txt", "textfile", "ocsp-proxy", "manual").</summary>
        public string ImportSource { get; set; } = string.Empty;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>Free-text admin notes.</summary>
        public string? Notes { get; set; }
    }
}
