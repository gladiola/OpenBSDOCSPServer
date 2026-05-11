# OpenBSDOCSPServer (Deutsch)

## Was dieses Programm macht
OpenBSDOCSPServer ist ein ASP.NET-Core-OCSP-Responder für OpenBSD-orientierte PKI-Betriebe.

Hauptfunktionen:
- Stellt OCSP-Antworten über `POST /ocsp` und `GET /ocsp/{base64url-request}` bereit.
- Signiert OCSP-Antworten mit Responder-Zertifikaten aus PFX- oder PEM-Dateien.
- Bietet eine authentifizierte Admin-Oberfläche zum Prüfen, Sperren/Freigeben und Kommentieren von Zertifikaten.
- Importiert Statusdaten aus OpenSSL-`index.txt`, Textdateien und Live-OCSP-Proxy-Sync.
- Speichert Zertifikatsstatus in SQLite.
- Unterstützt Sicherheitsfunktionen wie strikte Header, optionales mTLS und optionales Entra-ID-Admin-Login.

## Relevante RFCs
- RFC 6960 — OCSP.
- RFC 5019 — Lightweight OCSP Profile.
- RFC 8954 — OCSP Nonce Extension.

## Installation
1. .NET SDK 9.0 installieren.
2. Repository klonen.
3. Build: `dotnet build OcspServer/OcspServer.csproj`
4. Tests (empfohlen): `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. Start: `dotnet run --project OcspServer/OcspServer.csproj`

## Konfiguration
`OcspServer/appsettings.json` (oder Umgebungsvariablen/User-Secrets) konfigurieren:
- `FeatureFlags`: `EnableAdminAuth`, `EnableEntraIdAuth`, `EnableMtls`, `EnableSession`, `EnableSecurityHeaders`, `EnableIndexTxtWatch`
- `OcspServer`: `PfxPath`, `PfxPassword` oder `ResponderCertPath`, `SigningKeyPath`, `SigningKeyPassword`, außerdem Nonce/GET/POST/Update-Optionen
- `AdminAuth`: `AdminUsername`, `AdminPasswordHash`, `SessionTimeoutMinutes`
- `AzureAd` bei Entra-Modus
- `Ingestion`: `DatabasePath`, `IndexTxtWatchPath`, `PollingIntervalMinutes`, `LocalOcspResponderUrl`

## In Betrieb nehmen
1. Signiermaterial (PFX oder PEM) setzen.
2. Admin-Auth konfigurieren (lokal mit PBKDF2-Hash oder Entra ID).
3. `Ingestion.DatabasePath` setzen und App starten.
4. Über `/admin` Zertifikatsdaten importieren.
5. OCSP-Clients auf `POST /ocsp` oder `GET /ocsp/{base64url-encoded-der-request}` zeigen lassen.
6. Produktionssicherheit prüfen (HTTPS, Header, Auth, Ablaufdatum des Signaturzertifikats).
