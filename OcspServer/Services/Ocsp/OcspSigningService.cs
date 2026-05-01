using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using OcspServer.Models.Settings;

namespace OcspServer.Services.Ocsp
{
    /// <summary>
    /// Loads the OCSP signing certificate and private key from PEM files on disk.
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

            _sigAlgorithmName = _privateKey switch
            {
                RsaKeyParameters => "SHA256withRSA",
                ECPrivateKeyParameters => "SHA256withECDSA",
                _ => throw new NotSupportedException(
                    $"Unsupported signing key type: {_privateKey.GetType().Name}")
            };

            _logger.LogInformation("OCSP signing credentials loaded. Algorithm: {Alg}, Subject: {Subject}",
                _sigAlgorithmName, cert.SubjectDN);
        }

        private sealed class PasswordFinder : IPasswordFinder
        {
            private readonly char[] _password;
            public PasswordFinder(string password) => _password = password.ToCharArray();
            public char[] GetPassword() => _password;
        }
    }
}
