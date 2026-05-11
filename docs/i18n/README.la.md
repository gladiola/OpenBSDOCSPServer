# OpenBSDOCSPServer (Latina)

## Quid hoc programma facit
OpenBSDOCSPServer est OCSP responder in ASP.NET Core pro operationibus PKI more OpenBSD.

Praecipuae proprietates:
- Responsiones OCSP per `POST /ocsp` et `GET /ocsp/{base64url-request}`.
- Responsa signat credentialibus PFX vel PEM.
- Interfacies admin authentica ad inspectionem, revoke/reinstate, et annotationes.
- Importat data ex OpenSSL `index.txt`, text file, et OCSP proxy sync.
- Statum certificatorum in SQLite servat.
- Securitas aucta: security headers, mTLS optionale, Entra ID optionale.

## RFC applicabiles
- RFC 6960, RFC 5019, RFC 8954.

## Installatio
1. Installa .NET SDK 9.0.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Configuratio
Edita `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## In operationem inducere
1. Configura signing credentials (PFX vel PEM).
2. Configura admin auth (PBKDF2 locale vel Entra ID).
3. Pone `Ingestion.DatabasePath` et app incipias.
4. Importa data in `/admin`.
5. Dirige clientes ad `POST /ocsp` vel `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifica HTTPS, security headers, modum auth, et expiratio signer cert.
