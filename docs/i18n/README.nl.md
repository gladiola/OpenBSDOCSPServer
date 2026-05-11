# OpenBSDOCSPServer (Nederlands)

## Wat dit programma doet
OpenBSDOCSPServer is een ASP.NET Core OCSP-responder voor OpenBSD-achtige PKI-omgevingen.

Belangrijkste functies:
- OCSP-antwoorden via `POST /ocsp` en `GET /ocsp/{base64url-request}`.
- Ondertekent antwoorden met PFX- of PEM-credentials.
- Beveiligde admin-UI voor controle, intrekken/herstellen en notities.
- Import uit OpenSSL `index.txt`, tekstbestanden en OCSP proxy sync.
- Opslag van certificaatstatus in SQLite.
- Beveiligingshardening: headers, optionele mTLS, optionele Entra ID-auth.

## Toepasselijke RFC's
- RFC 6960, RFC 5019, RFC 8954.

## Installatie
1. Installeer .NET SDK 9.0.
2. Clone de repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Configuratie
Bewerk `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## In gebruik nemen
1. Stel ondertekeningscredentials in (PFX of PEM).
2. Stel admin-auth in (lokale PBKDF2 of Entra ID).
3. Stel `Ingestion.DatabasePath` in en start de app.
4. Importeer certificaatrecords via `/admin`.
5. Richt clients op `POST /ocsp` of `GET /ocsp/{base64url-encoded-der-request}`.
6. Controleer HTTPS, security headers, auth-modus en vervaldatum van het ondertekeningscertificaat.
