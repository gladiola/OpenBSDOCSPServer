namespace OcspServer.Models.Settings
{
    /// <summary>
    /// Local (air-gapped) admin authentication settings.
    /// Password is stored as a PBKDF2-hashed value.
    /// </summary>
    public class AdminAuthSettings
    {
        public string AdminUsername { get; set; } = "admin";

        /// <summary>
        /// PBKDF2-HMAC-SHA256 hash of the admin password.
        /// Format: "iterations:base64salt:base64hash"
        /// </summary>
        public string AdminPasswordHash { get; set; } = string.Empty;

        /// <summary>Session idle timeout in minutes.</summary>
        public int SessionTimeoutMinutes { get; set; } = 30;
    }
}
