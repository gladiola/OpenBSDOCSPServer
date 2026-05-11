# OpenBSDOCSPServer (Gaeilge)

## Cad a dhéanann an clár seo
Is freastalaí OCSP é OpenBSDOCSPServer ar ASP.NET Core do PKI i stíl OpenBSD.

Príomhghnéithe:
- Freagraí OCSP ar `POST /ocsp` agus `GET /ocsp/{base64url-request}`.
- Sínithe ar fhreagraí le dintiúir PFX nó PEM.
- Admin UI fíordheimhnithe do iniúchadh, revoke/reinstate, agus nótaí.
- Iompórtáil ó OpenSSL `index.txt`, text file, agus OCSP proxy sync.
- Stóráil stádas teastais i SQLite.
- Cruasú slándála: security headers, mTLS roghnach, Entra ID roghnach.

## RFCanna infheidhme
- RFC 6960, RFC 5019, RFC 8954.

## Suiteáil
1. Suiteáil .NET SDK 9.0.
2. Clone an repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Cumraíocht
Cuir in eagar `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Cur i bhfeidhm
1. Socraigh signing credentials (PFX nó PEM).
2. Socraigh admin auth (PBKDF2 áitiúil nó Entra ID).
3. Socraigh `Ingestion.DatabasePath` agus tosnaigh an aip.
4. Iompórtáil taifid i `/admin`.
5. Dírigh cliaint ar `POST /ocsp` nó `GET /ocsp/{base64url-encoded-der-request}`.
6. Deimhnigh HTTPS, security headers, modh auth, agus éag signer cert.
