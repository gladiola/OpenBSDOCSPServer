# OpenBSDOCSPServer (Svenska)

## Vad programmet gör
OpenBSDOCSPServer är en ASP.NET Core-baserad OCSP-responder för OpenBSD-liknande PKI-drift.

Huvudfunktioner:
- OCSP-svar via `POST /ocsp` och `GET /ocsp/{base64url-request}`.
- Signerar svar med PFX- eller PEM-uppgifter.
- Autentiserad admin-UI för granskning, revoke/reinstate och anteckningar.
- Import från OpenSSL `index.txt`, textfil och OCSP proxy sync.
- Lagring av certifikatstatus i SQLite.
- Säkerhetshärdning: security headers, valfri mTLS, valfri Entra ID.

## Tillämpliga RFC
- RFC 6960, RFC 5019, RFC 8954.

## Installation
1. Installera .NET SDK 9.0.
2. Klona repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfiguration
Redigera `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Driftsättning
1. Konfigurera signeringsuppgifter (PFX eller PEM).
2. Konfigurera admin-auth (lokal PBKDF2 eller Entra ID).
3. Sätt `Ingestion.DatabasePath` och starta appen.
4. Importera certifikatposter via `/admin`.
5. Peka klienter mot `POST /ocsp` eller `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifiera HTTPS, security headers, auth-läge och signer cert expiry.
