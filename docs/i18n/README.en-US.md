# OpenBSDOCSPServer (US-English)

## What this program does
OpenBSDOCSPServer is an ASP.NET Core OCSP responder for OpenBSD-style PKI operations.

Main features:
- Serves OCSP responses over `POST /ocsp` and `GET /ocsp/{base64url-request}`.
- Signs OCSP responses using responder credentials from PFX or PEM files.
- Provides an authenticated admin UI to review certificates, revoke/reinstate status, and add notes.
- Imports certificate status data from OpenSSL `index.txt`, simple text files, and live OCSP proxy sync.
- Stores certificate status data in SQLite.
- Supports security hardening features such as strict headers, optional mTLS, and optional Entra ID admin authentication.

## Applicable RFC references
- RFC 6960 — X.509 Internet Public Key Infrastructure Online Certificate Status Protocol (OCSP).
- RFC 5019 — The Lightweight Online Certificate Status Protocol (OCSP) Profile for High-Volume Environments.
- RFC 8954 — OCSP Nonce Extension.

## Installation
1. Install .NET SDK 9.0.
2. Clone this repository.
3. Build the server:
   - `dotnet build OcspServer/OcspServer.csproj`
4. Run tests (recommended):
   - `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. Run the application:
   - `dotnet run --project OcspServer/OcspServer.csproj`

## Configuration
Edit `OcspServer/appsettings.json` (or environment variables/user secrets) for these sections:

- `FeatureFlags`
  - `EnableAdminAuth`, `EnableEntraIdAuth`, `EnableMtls`, `EnableSession`, `EnableSecurityHeaders`, `EnableIndexTxtWatch`
- `OcspServer`
  - `PfxPath`, `PfxPassword` (optional)
  - or `ResponderCertPath`, `SigningKeyPath`, `SigningKeyPassword`
  - `NextUpdateHours`, `AllowNonce`, `RequireNonce`, `MaxNonceSizeBytes`, `AllowGetRequests`, `AllowPostRequests`
- `AdminAuth`
  - `AdminUsername`, `AdminPasswordHash`, `SessionTimeoutMinutes`
- `AzureAd` (only when `EnableEntraIdAuth` is true)
  - `Instance`, `TenantId`, `ClientId`, `ClientSecret`, `AdminGroupId`, `CallbackPath`, `SignedOutCallbackPath`
- `Ingestion`
  - `DatabasePath`, `IndexTxtWatchPath`, `PollingIntervalMinutes`, `LocalOcspResponderUrl`

## Configure into operation
1. Prepare OCSP signing credentials:
   - Either provide `OcspServer.PfxPath` (+ optional password),
   - or provide PEM files at `OcspServer.ResponderCertPath` and `OcspServer.SigningKeyPath`.
2. Configure admin authentication:
   - Local admin mode: set `FeatureFlags.EnableAdminAuth=true` and set `AdminAuth.AdminPasswordHash` in PBKDF2 format (`iterations:base64salt:base64hash`).
   - Entra mode: set `FeatureFlags.EnableEntraIdAuth=true` and fill `AzureAd` settings.
3. Set `Ingestion.DatabasePath` (default `ocsp.db`) and start the app.
4. Open the admin UI at `/admin` and import certificate records (`index.txt`, text file, or OCSP proxy sync).
5. Point OCSP clients to:
   - `POST /ocsp` with `application/ocsp-request`, or
   - `GET /ocsp/{base64url-encoded-der-request}`
6. Verify production settings (HTTPS, security headers, auth mode, and signer cert expiry monitoring in dashboard).
