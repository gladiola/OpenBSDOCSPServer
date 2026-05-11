# OpenBSDOCSPServer (Kreyòl Ayisyen)

## Kisa pwogram sa a fè
OpenBSDOCSPServer se yon repondè OCSP sou ASP.NET Core pou operasyon PKI stil OpenBSD.

Fonksyon prensipal:
- Bay repons OCSP sou `POST /ocsp` ak `GET /ocsp/{base64url-request}`.
- Siyen repons yo ak kalifikasyon PFX oswa PEM.
- Bay yon UI admin ki otantifye pou revize sètifika, revoke/retabli, epi ajoute nòt.
- Enpòte done soti nan OpenSSL `index.txt`, fichye tèks, ak OCSP proxy sync.
- Sere estati sètifika nan SQLite.
- Sipòte sekirite ranfòse: headers sekirite, mTLS opsyonèl, Entra ID opsyonèl.

## RFC ki aplikab
- RFC 6960, RFC 5019, RFC 8954.

## Enstalasyon
1. Enstale .NET SDK 9.0.
2. Klone depo a.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigirasyon
Modifye `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Mete nan operasyon
1. Mete kredansyèl siyati (PFX oswa PEM).
2. Mete otantifikasyon admin (PBKDF2 lokal oswa Entra ID).
3. Mete `Ingestion.DatabasePath` epi lanse aplikasyon an.
4. Enpòte dosye sètifika nan `/admin`.
5. Voye kliyan OCSP yo sou `POST /ocsp` oswa `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifye HTTPS, headers sekirite, mòd auth, ak ekspirasyon sètifika siyatè a.
