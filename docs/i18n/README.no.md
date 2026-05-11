# OpenBSDOCSPServer (Norsk)

## Hva programmet gjør
OpenBSDOCSPServer er en ASP.NET Core OCSP-responder for OpenBSD-lignende PKI-drift.

Hovedfunksjoner:
- OCSP-svar via `POST /ocsp` og `GET /ocsp/{base64url-request}`.
- Signerer svar med PFX- eller PEM-legitimasjon.
- Autentisert admin-UI for kontroll, revoke/reinstate og notater.
- Import fra OpenSSL `index.txt`, tekstfil og OCSP proxy sync.
- Lagrer sertifikatstatus i SQLite.
- Sikkerhetsherding: security headers, valgfri mTLS, valgfri Entra ID.

## Aktuelle RFC-er
- RFC 6960, RFC 5019, RFC 8954.

## Installasjon
1. Installer .NET SDK 9.0.
2. Klon repoet.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigurasjon
Rediger `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Sette i drift
1. Konfigurer signeringslegitimasjon (PFX eller PEM).
2. Konfigurer admin-auth (lokal PBKDF2 eller Entra ID).
3. Sett `Ingestion.DatabasePath` og start appen.
4. Importer sertifikatdata i `/admin`.
5. Pek klienter til `POST /ocsp` eller `GET /ocsp/{base64url-encoded-der-request}`.
6. Kontroller HTTPS, security headers, auth-modus og signer cert expiry.
