namespace OcspServer.Models.Settings
{
    /// <summary>
    /// Configuration for the OCSP responder engine and signing credentials.
    /// </summary>
    public class OcspServerSettings
    {
        /// <summary>Path to the PEM-encoded OCSP signing certificate issued by the offline CA.</summary>
        public string ResponderCertPath { get; set; } = string.Empty;

        /// <summary>Path to the PEM-encoded OCSP signing private key.</summary>
        public string SigningKeyPath { get; set; } = string.Empty;

        /// <summary>Optional passphrase for the signing private key.</summary>
        public string? SigningKeyPassword { get; set; }

        /// <summary>
        /// Hours between thisUpdate and nextUpdate in OCSP responses.
        /// Clients must not cache responses past nextUpdate.
        /// </summary>
        public int NextUpdateHours { get; set; } = 24;

        /// <summary>Allow nonce extension in OCSP requests/responses (RFC 8954).</summary>
        public bool AllowNonce { get; set; } = true;

        /// <summary>Require nonce extension; return unauthorized if absent.</summary>
        public bool RequireNonce { get; set; } = false;

        /// <summary>Maximum nonce size in bytes (RFC 8954 §2.1 mandates ≤ 32 bytes).</summary>
        public int MaxNonceSizeBytes { get; set; } = 32;

        /// <summary>Accept HTTP GET requests with base64url-encoded request (RFC 5019).</summary>
        public bool AllowGetRequests { get; set; } = true;

        /// <summary>Accept HTTP POST requests (RFC 6960 §4.1).</summary>
        public bool AllowPostRequests { get; set; } = true;
    }
}
