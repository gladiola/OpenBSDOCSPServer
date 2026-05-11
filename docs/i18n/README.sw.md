# OpenBSDOCSPServer (Kiswahili)

## Programu hii inafanya nini
OpenBSDOCSPServer ni huduma ya OCSP ya ASP.NET Core kwa uendeshaji wa PKI wa mtindo wa OpenBSD.

Vipengele kuu:
- Hutoa majibu ya OCSP kupitia `POST /ocsp` na `GET /ocsp/{base64url-request}`.
- Husaini majibu kwa kutumia sifa za PFX au PEM.
- Ina UI ya msimamizi yenye uthibitisho kwa ukaguzi, kufuta/rejesha vyeti, na maelezo.
- Huagiza hali kutoka OpenSSL `index.txt`, faili za maandishi, na OCSP proxy sync.
- Huhifadhi data kwenye SQLite.
- Ina usalama wa ziada: vichwa vya usalama, mTLS ya hiari, na Entra ID ya hiari.

## RFC husika
- RFC 6960, RFC 5019, RFC 8954.

## Usakinishaji
1. Sakinisha .NET SDK 9.0.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Usanidi
Hariri `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Kuweka kwenye uzalishaji
1. Weka credential za kusaini (PFX au PEM).
2. Weka admin auth (PBKDF2 ya ndani au Entra ID).
3. Weka `Ingestion.DatabasePath` na uanzishe app.
4. Ingiza rekodi kupitia `/admin`.
5. Elekeza wateja kwa `POST /ocsp` au `GET /ocsp/{base64url-encoded-der-request}`.
6. Hakiki HTTPS, security headers, auth mode, na tarehe ya kuisha ya cheti cha kusaini.
