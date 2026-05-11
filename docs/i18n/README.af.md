# OpenBSDOCSPServer (Afrikaans)

## Wat hierdie program doen
OpenBSDOCSPServer is ’n ASP.NET Core OCSP-responder vir OpenBSD-styl PKI-bedrywighede.

Hooffunksies:
- Bedien OCSP-antwoorde via `POST /ocsp` en `GET /ocsp/{base64url-request}`.
- Onderteken antwoorde met PFX- of PEM-geloofsbriewe.
- Geverifieerde admin-UI vir sertifikaatkontrole, revoke/reinstate en notas.
- Invoer vanaf OpenSSL `index.txt`, tekslêers en OCSP proxy sync.
- Berg sertifikaatstatus in SQLite.
- Sekuriteitshardening: streng headers, opsionele mTLS, opsionele Entra ID.

## Toepaslike RFC's
- RFC 6960, RFC 5019, RFC 8954.

## Installasie
1. Installeer .NET SDK 9.0.
2. Klone die repo.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigurasie
Wysig `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Inbedryfstelling
1. Stel teken-geloofsbriewe op (PFX of PEM).
2. Stel admin-auth op (plaaslike PBKDF2 of Entra ID).
3. Stel `Ingestion.DatabasePath` en begin die toepassing.
4. Voer sertifikaatdata in by `/admin`.
5. Rig kliënte na `POST /ocsp` of `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifieer HTTPS, headers, auth-modus en vervaldatum van die tekensertifikaat.
