namespace OcspServer.Models.Settings
{
    public class FeatureFlags
    {
        public bool EnableSession { get; set; } = true;
        public bool EnableSecurityHeaders { get; set; } = true;
        public bool EnableMtls { get; set; } = false;
        public bool EnableAdminAuth { get; set; } = true;
        public bool EnableOcspNonce { get; set; } = true;

        /// <summary>
        /// When true, admin UI authentication is delegated to Azure Entra ID via
        /// OpenID Connect.  Requires a network path to the EntraID tenant.
        /// When false (default), local PBKDF2 cookie auth is used instead.
        /// </summary>
        public bool EnableEntraIdAuth { get; set; } = false;

        /// <summary>
        /// When true, a background service watches the file at
        /// <see cref="IngestionSettings.IndexTxtWatchPath"/> and automatically
        /// re-imports it whenever the file changes.
        /// When false (default), import is manual-only.
        /// </summary>
        public bool EnableIndexTxtWatch { get; set; } = false;
    }
}
