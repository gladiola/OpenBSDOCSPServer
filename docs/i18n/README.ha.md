# OpenBSDOCSPServer (Hausa)

## Abin da wannan shiri yake yi
OpenBSDOCSPServer sabis ne na OCSP da aka gina da ASP.NET Core don tsarin PKI irin na OpenBSD.

Manyan siffofi:
- Yana amsa OCSP ta `POST /ocsp` da `GET /ocsp/{base64url-request}`.
- Yana sanya hannu da takardun PFX ko PEM.
- Yana da admin UI mai kariya don duba takardu, revoke/reinstate, da rubuta bayanai.
- Yana shigo da bayanai daga OpenSSL `index.txt`, text file, da OCSP proxy sync.
- Yana adana matsayin takardu a SQLite.
- Tsaro: security headers, mTLS na zaɓi, Entra ID na zaɓi.

## RFC masu dacewa
- RFC 6960, RFC 5019, RFC 8954.

## Shigarwa
1. Sanya .NET SDK 9.0.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Saitawa
Gyara `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Sanya cikin aiki
1. Saita signing credentials (PFX ko PEM).
2. Saita admin auth (PBKDF2 na gida ko Entra ID).
3. Saita `Ingestion.DatabasePath` sannan ka fara app.
4. Shigo da bayanai ta `/admin`.
5. Nuna clients zuwa `POST /ocsp` ko `GET /ocsp/{base64url-encoded-der-request}`.
6. Duba HTTPS, security headers, auth mode, da ranar karewar signer cert.
