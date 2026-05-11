# OpenBSDOCSPServer (Gagana Sāmoa)

## O le ā le galuega a lenei polokalame
OpenBSDOCSPServer o se OCSP responder i luga o ASP.NET Core mo PKI faiga OpenBSD.

Vaega autū:
- Tali OCSP i `POST /ocsp` ma `GET /ocsp/{base64url-request}`.
- Saini tali i credential PFX po o PEM.
- Admin UI ua puipuia mo le iloiloina, revoke/reinstate, ma notes.
- Faaulufale mai mai OpenSSL `index.txt`, text file, ma OCSP proxy sync.
- Teu faamaumauga i SQLite.
- Saogalemu faamalosia: security headers, mTLS filifiliga, Entra ID filifiliga.

## RFC talafeagai
- RFC 6960, RFC 5019, RFC 8954.

## Faapipiiina
1. Faapipii .NET SDK 9.0.
2. Clone le repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Faiga seti
Faasa'o `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Faagaoioi
1. Seti signing credentials (PFX po o PEM).
2. Seti admin auth (PBKDF2 local po o Entra ID).
3. Seti `Ingestion.DatabasePath` ma amata le app.
4. Faaaoga `/admin` e import ai faamaumauga.
5. Faasino clients i `POST /ocsp` po o `GET /ocsp/{base64url-encoded-der-request}`.
6. Siaki HTTPS, security headers, auth mode, ma le aso e muta ai signer cert.
