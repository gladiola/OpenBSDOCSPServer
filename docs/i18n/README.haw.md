# OpenBSDOCSPServer (ʻŌlelo Hawaiʻi)

## He aha ka hana a kēia papahana
He OCSP responder ʻo OpenBSDOCSPServer ma ASP.NET Core no ka hana PKI ʻano OpenBSD.

Nā hiʻohiʻona nui:
- Pane OCSP ma `POST /ocsp` a me `GET /ocsp/{base64url-request}`.
- Kākau inoa i nā pane me nā credential PFX a i ʻole PEM.
- Aia he admin UI me ka hōʻoia no ka nānā palapala, revoke/reinstate, a me nā memo.
- Hoʻokomo ʻikepili mai OpenSSL `index.txt`, text file, a me OCSP proxy sync.
- Mālama i ka ʻike ma SQLite.
- Kākoʻo i ka palekana: security headers, mTLS koho, Entra ID koho.

## Nā RFC pili
- RFC 6960, RFC 5019, RFC 8954.

## Hoʻokomo
1. Hoʻokomo i ka .NET SDK 9.0.
2. Clone i ka repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Hoʻonohonoho
Hoʻoponopono i `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Hoʻomākaukau no ka hana
1. Hoʻonohonoho i nā signing credential (PFX a i ʻole PEM).
2. Hoʻonohonoho i ka admin auth (PBKDF2 kūloko a i ʻole Entra ID).
3. Hoʻonohonoho `Ingestion.DatabasePath` a holo i ka app.
4. Hoʻokomo i nā moʻokāki ma `/admin`.
5. Kuhikuhi i nā OCSP clients i `POST /ocsp` a i ʻole `GET /ocsp/{base64url-encoded-der-request}`.
6. Nānā i HTTPS, security headers, auth mode, a me ka lā pau o ka signer cert.
