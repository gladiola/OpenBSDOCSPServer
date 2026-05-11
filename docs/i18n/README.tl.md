# OpenBSDOCSPServer (Tagalog)

## Ano ang ginagawa ng program na ito
Ang OpenBSDOCSPServer ay isang ASP.NET Core OCSP responder para sa OpenBSD-style PKI operations.

Pangunahing tampok:
- Sumasagot sa `POST /ocsp` at `GET /ocsp/{base64url-request}`.
- Nagsa-sign ng OCSP responses gamit ang PFX o PEM credentials.
- May authenticated admin UI para sa review, revoke/reinstate, at notes.
- Nag-iimport mula sa OpenSSL `index.txt`, text file, at OCSP proxy sync.
- Nagtatago ng certificate status sa SQLite.
- May security hardening: security headers, optional mTLS, optional Entra ID.

## Mga RFC na naaangkop
- RFC 6960, RFC 5019, RFC 8954.

## Pag-install
1. I-install ang .NET SDK 9.0.
2. I-clone ang repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Configuration
I-edit ang `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Pagsasaayos para umandar
1. I-set ang signing credentials (PFX o PEM).
2. I-set ang admin auth (local PBKDF2 o Entra ID).
3. I-set ang `Ingestion.DatabasePath` at paandarin ang app.
4. Mag-import ng records sa `/admin`.
5. Ituro ang clients sa `POST /ocsp` o `GET /ocsp/{base64url-encoded-der-request}`.
6. Suriin ang HTTPS, security headers, auth mode, at expiry ng signer cert.
