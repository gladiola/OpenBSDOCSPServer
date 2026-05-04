namespace OcspServer.Models.Settings
{
    /// <summary>
    /// Azure Entra ID (Azure AD) configuration used when
    /// <see cref="FeatureFlags.EnableEntraIdAuth"/> is true.
    ///
    /// These values mirror the AzureAd section consumed by
    /// Microsoft.Identity.Web's <c>AddMicrosoftIdentityWebApp</c>.
    /// </summary>
    public class AzureAdSettings
    {
        /// <summary>AAD authority base, e.g. "https://login.microsoftonline.com/".</summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>Directory (tenant) ID GUID.</summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>Application (client) ID GUID registered in the Entra tenant.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret for confidential-client auth code flow.
        /// Store in user secrets or environment variables, not appsettings.json.
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Optional Entra security group object ID whose members are granted admin access.
        /// When null or empty, any authenticated Entra user can access the admin UI.
        /// </summary>
        public string? AdminGroupId { get; set; }

        /// <summary>
        /// Callback path for the OIDC redirect.
        /// Defaults to "/signin-oidc" as required by Microsoft.Identity.Web.
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-oidc";

        /// <summary>
        /// Sign-out callback path.
        /// </summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-oidc";
    }
}
