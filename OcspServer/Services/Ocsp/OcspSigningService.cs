using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using OcspServer.Models.Settings;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OcspServer.Services.Ocsp
{
    /// <summary>
    /// Loads the OCSP signing certificate and private key from either:
    ///   • a PKCS#12 (.pfx) file (when <see cref="OcspServerSettings.PfxPath"/> is set), or
    ///   • PEM files on disk (legacy path via <see cref="OcspServerSettings.ResponderCertPath"/>
    ///     / <see cref="OcspServerSettings.SigningKeyPath"/>).
    ///
    /// The certificate must have been issued by the offline OpenBSD CA and should
    /// carry the id-kp-OCSPSigning extended key usage (OID 1.3.6.1.5.5.7.3.9).
    ///
    /// Thread-safe: credentials are loaded once at startup.
    /// </summary>
    public class OcspSigningService
    {
        private readonly OcspServerSettings _settings;
        private readonly ILogger<OcspSigningService> _logger;

        // Cached on first call to GetSigningMaterial()
        private AsymmetricKeyParameter? _publicKey;
        private AsymmetricKeyParameter? _privateKey;
        private Org.BouncyCastle.X509.X509Certificate[]? _chain;
        private string? _sigAlgorithmName;
        private readonly object _lock = new();

        public OcspSigningService(OcspServerSettings settings, ILogger<OcspSigningService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Returns signing algorithm name, private key, responder public key, and certificate chain.
        /// Loads from disk on first call; subsequent calls return cached values.
        /// </summary>
        public (string SigAlgName, AsymmetricKeyParameter PrivateKey,
                AsymmetricKeyParameter PublicKey,
                Org.BouncyCastle.X509.X509Certificate[] Chain) GetSigningMaterial()
        {
            lock (_lock)
            {
                if (_privateKey == null || _chain == null)
                    Load();

                return (_sigAlgorithmName!, _privateKey!, _publicKey!, _chain!);
            }
        }

        private void Load()
        {
            if (!string.IsNullOrWhiteSpace(_settings.PfxPath))
            {
                LoadFromPfx();
            }
            else
            {
                LoadFromPem();
            }
        }

        // ── PFX / PKCS#12 loader ─────────────────────────────────────────────

        private void LoadFromPfx()
        {
            _logger.LogInformation("Loading OCSP signing credentials from PFX {PfxPath}",
                _settings.PfxPath);

            // X509CertificateLoader is the .NET 9 recommended API for loading PFX files.
            // LoadPkcs12FromFile accepts an optional password and storage flags; we request
            // Exportable so we can extract the raw key bytes for BouncyCastle conversion.
            var flags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet;
            using var dotNetCert = System.Security.Cryptography.X509Certificates
                .X509CertificateLoader.LoadPkcs12FromFile(
                    _settings.PfxPath!,
                    _settings.PfxPassword,
                    flags);

            // Convert the .NET cert to a BouncyCastle cert.
            var bcCert = DotNetUtilities.FromX509Certificate(dotNetCert);

            // Extract and convert the private key to BouncyCastle.
            AsymmetricKeyParameter? bcPrivateKey = null;

            var rsaKey = dotNetCert.GetRSAPrivateKey();
            if (rsaKey != null)
            {
                // Export as PKCS#8 DER, then import into BouncyCastle.
                byte[] pkcs8Der = rsaKey.ExportPkcs8PrivateKey();
                bcPrivateKey = PrivateKeyFactory.CreateKey(pkcs8Der);
            }
            else
            {
                var ecdsaKey = dotNetCert.GetECDsaPrivateKey();
                if (ecdsaKey != null)
                {
                    byte[] pkcs8Der = ecdsaKey.ExportPkcs8PrivateKey();
                    bcPrivateKey = PrivateKeyFactory.CreateKey(pkcs8Der);
                }
            }

            if (bcPrivateKey == null)
                throw new InvalidOperationException(
                    $"Could not extract RSA or ECDSA private key from PFX {_settings.PfxPath}");

            _privateKey = bcPrivateKey;
            _publicKey = bcCert.GetPublicKey();
            _chain = new[] { bcCert };
            _sigAlgorithmName = ResolveSigningAlgorithm(_privateKey);

            _logger.LogInformation(
                "OCSP signing credentials loaded from PFX. Algorithm: {Alg}, Subject: {Subject}",
                _sigAlgorithmName, bcCert.SubjectDN);
        }

        // ── PEM loader ────────────────────────────────────────────────────────

        private void LoadFromPem()
        {
            _logger.LogInformation("Loading OCSP signing credentials from {CertPath} / {KeyPath}",
                _settings.ResponderCertPath, _settings.SigningKeyPath);

            // Load certificate
            Org.BouncyCastle.X509.X509Certificate cert;
            using (var certReader = new StreamReader(_settings.ResponderCertPath))
            {
                var pem = new PemReader(certReader);
                cert = (Org.BouncyCastle.X509.X509Certificate)pem.ReadObject()
                    ?? throw new InvalidOperationException(
                        $"Could not read certificate from {_settings.ResponderCertPath}");
            }

            // Load private key
            AsymmetricCipherKeyPair? keyPair;
            using (var keyReader = new StreamReader(_settings.SigningKeyPath))
            {
                var pem = string.IsNullOrEmpty(_settings.SigningKeyPassword)
                    ? new PemReader(keyReader)
                    : new PemReader(keyReader, new PasswordFinder(_settings.SigningKeyPassword));

                var obj = pem.ReadObject()
                    ?? throw new InvalidOperationException(
                        $"Could not read private key from {_settings.SigningKeyPath}");

                keyPair = obj as AsymmetricCipherKeyPair
                    ?? throw new InvalidOperationException(
                        $"PEM object from {_settings.SigningKeyPath} is not a key pair");
            }

            _privateKey = keyPair.Private;
            _publicKey = cert.GetPublicKey();
            _chain = new[] { cert };
            _sigAlgorithmName = ResolveSigningAlgorithm(_privateKey);

            _logger.LogInformation("OCSP signing credentials loaded. Algorithm: {Alg}, Subject: {Subject}",
                _sigAlgorithmName, cert.SubjectDN);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string ResolveSigningAlgorithm(AsymmetricKeyParameter key) =>
            key switch
            {
                RsaKeyParameters => "SHA256withRSA",
                ECPrivateKeyParameters => "SHA256withECDSA",
                _ => throw new NotSupportedException(
                    $"Unsupported signing key type: {key.GetType().Name}")
            };

        private sealed class PasswordFinder : IPasswordFinder
        {
            private readonly char[] _password;
            public PasswordFinder(string password) => _password = password.ToCharArray();
            public char[] GetPassword() => _password;
        }
    }
}
