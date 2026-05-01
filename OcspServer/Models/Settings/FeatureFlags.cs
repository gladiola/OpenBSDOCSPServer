namespace OcspServer.Models.Settings
{
    public class FeatureFlags
    {
        public bool EnableSession { get; set; } = true;
        public bool EnableSecurityHeaders { get; set; } = true;
        public bool EnableMtls { get; set; } = false;
        public bool EnableAdminAuth { get; set; } = true;
        public bool EnableOcspNonce { get; set; } = true;
    }
}
