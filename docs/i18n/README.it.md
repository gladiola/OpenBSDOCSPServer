# OpenBSDOCSPServer (Italiano)

## Cosa fa questo programma
OpenBSDOCSPServer è un responder OCSP ASP.NET Core per operazioni PKI in stile OpenBSD.

Funzioni principali:
- Endpoint OCSP `POST /ocsp` e `GET /ocsp/{base64url-request}`.
- Firma risposte OCSP con credenziali PFX o PEM.
- UI admin autenticata per controllo certificati, revoca/ripristino e note.
- Import da `index.txt` OpenSSL, file di testo e sync proxy OCSP.
- Archivio stato certificati in SQLite.
- Hardening: header di sicurezza, mTLS opzionale, Entra ID opzionale.

## RFC applicabili
- RFC 6960, RFC 5019, RFC 8954.

## Installazione
1. Installa .NET SDK 9.0.
2. Clona il repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Configurazione
Modifica `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Messa in esercizio
1. Configura credenziali di firma (PFX o PEM).
2. Configura auth admin (PBKDF2 locale o Entra ID).
3. Imposta `Ingestion.DatabasePath` e avvia.
4. Importa record in `/admin`.
5. Punta i client a `POST /ocsp` o `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifica HTTPS, header, autenticazione e scadenza certificato firmatario.
