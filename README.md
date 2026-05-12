# OpenBSDOCSPServer

## Documentation language index

- [US-English](docs/i18n/README.en-US.md)
- [Deutsch](docs/i18n/README.de.md)
- [Español](docs/i18n/README.es.md)
- [Français](docs/i18n/README.fr.md)
- [Português](docs/i18n/README.pt.md)
- [Italiano](docs/i18n/README.it.md)
- [繁體中文（香港）](docs/i18n/README.zh-HK.md)
- [한국어](docs/i18n/README.ko.md)
- [हिन्दी](docs/i18n/README.hi.md)
- [Русский](docs/i18n/README.ru.md)
- [العربية](docs/i18n/README.ar.md)
- [Kiswahili](docs/i18n/README.sw.md)
- [日本語](docs/i18n/README.ja.md)
- [Kreyòl Ayisyen](docs/i18n/README.ht.md)
- [ʻŌlelo Hawaiʻi](docs/i18n/README.haw.md)
- [Gagana Sāmoa](docs/i18n/README.sm.md)
- [Te Reo Māori](docs/i18n/README.mi.md)
- [Afrikaans](docs/i18n/README.af.md)
- [Nederlands](docs/i18n/README.nl.md)
- [Hausa](docs/i18n/README.ha.md)
- [አማርኛ](docs/i18n/README.am.md)
- [Yorùbá](docs/i18n/README.yo.md)
- [বাংলা](docs/i18n/README.bn.md)
- [简体中文](docs/i18n/README.zh-CN.md)
- [Eesti](docs/i18n/README.et.md)
- [Suomi](docs/i18n/README.fi.md)
- [Svenska](docs/i18n/README.sv.md)
- [Norsk](docs/i18n/README.no.md)
- [Українська](docs/i18n/README.uk.md)
- [ไทย](docs/i18n/README.th.md)
- [Bahasa Indonesia](docs/i18n/README.id.md)
- [Tagalog](docs/i18n/README.tl.md)
- [Bahasa Melayu](docs/i18n/README.ms.md)
- [Basa Jawa](docs/i18n/README.jv.md)
- [Ελληνικά](docs/i18n/README.el.md)
- [Latina](docs/i18n/README.la.md)
- [עברית](docs/i18n/README.he.md)
- [Gaeilge](docs/i18n/README.ga.md)

# OpenBSDOCSPServer (US-English)

## What this program does
OpenBSDOCSPServer is an ASP.NET Core OCSP responder for OpenBSD-style PKI operations.

Main features:
- Serves OCSP responses over `POST /ocsp` and `GET /ocsp/{base64url-request}`.
- Signs OCSP responses using responder credentials from PFX or PEM files.
- Provides an authenticated admin UI to review certificates, revoke/reinstate status, and add notes.
- Imports certificate status data from OpenSSL `index.txt`, simple text files, and live OCSP proxy sync.
- Stores certificate status data in SQLite.
- Supports security hardening features such as strict headers, optional mTLS, and optional Entra ID admin authentication.
- Supports localized MVC UI text with a language selector in the top navigation.

## UI localization

The web UI supports the following cultures (selectable from the **Language** menu in the site header):

`en-US`, `de-DE`, `es-ES`, `fr-FR`, `pt-PT`, `it-IT`, `zh-HK`, `ko-KR`, `hi-IN`, `ru-RU`, `ar-SA`, `sw-KE`, `ja-JP`, `ht-HT`, `haw-US`, `sm-WS`, `mi-NZ`, `af-ZA`, `nl-NL`, `ha-NG`, `am-ET`, `yo-NG`, `bn-BD`, `zh-CN`, `et-EE`, `fi-FI`, `sv-SE`, `nb-NO`, `uk-UA`, `th-TH`, `id-ID`, `tl-PH`, `ms-MY`, `jv-ID`, `el-GR`, `la-VA`, `he-IL`, `ga-IE`.

Language preference is stored with the ASP.NET Core culture cookie and reused on later requests.

## Applicable RFC references
- RFC 6960 — X.509 Internet Public Key Infrastructure Online Certificate Status Protocol (OCSP).
- RFC 5019 — The Lightweight Online Certificate Status Protocol (OCSP) Profile for High-Volume Environments.
- RFC 8954 — OCSP Nonce Extension.

## Installation
1. Install .NET SDK 9.0.
2. Clone this repository.
3. Build the server:
   - `dotnet build OcspServer/OcspServer.csproj`
4. Run tests (recommended):
   - `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. Run the application:
   - `dotnet run --project OcspServer/OcspServer.csproj`

## Configuration
Edit `OcspServer/appsettings.json` (or environment variables/user secrets) for these sections:

- `FeatureFlags`
  - `EnableAdminAuth`, `EnableEntraIdAuth`, `EnableMtls`, `EnableSession`, `EnableSecurityHeaders`, `EnableIndexTxtWatch`
- `OcspServer`
  - `PfxPath`, `PfxPassword` (optional)
  - or `ResponderCertPath`, `SigningKeyPath`, `SigningKeyPassword`
  - `NextUpdateHours`, `AllowNonce`, `RequireNonce`, `MaxNonceSizeBytes`, `AllowGetRequests`, `AllowPostRequests`
- `AdminAuth`
  - `AdminUsername`, `AdminPasswordHash`, `SessionTimeoutMinutes`
- `AzureAd` (only when `EnableEntraIdAuth` is true)
  - `Instance`, `TenantId`, `ClientId`, `ClientSecret`, `AdminGroupId`, `CallbackPath`, `SignedOutCallbackPath`
- `Ingestion`
  - `DatabasePath`, `IndexTxtWatchPath`, `PollingIntervalMinutes`, `LocalOcspResponderUrl`

## Configure into operation
1. Prepare OCSP signing credentials:
   - Either provide `OcspServer.PfxPath` (+ optional password),
   - or provide PEM files at `OcspServer.ResponderCertPath` and `OcspServer.SigningKeyPath`.
2. Configure admin authentication:
   - Local admin mode: set `FeatureFlags.EnableAdminAuth=true` and set `AdminAuth.AdminPasswordHash` in PBKDF2 format (`iterations:base64salt:base64hash`).
   - Entra mode: set `FeatureFlags.EnableEntraIdAuth=true` and fill `AzureAd` settings.
3. Set `Ingestion.DatabasePath` (default `ocsp.db`) and start the app.
4. Open the admin UI at `/admin` and import certificate records (`index.txt`, text file, or OCSP proxy sync).
5. Point OCSP clients to:
   - `POST /ocsp` with `application/ocsp-request`, or
   - `GET /ocsp/{base64url-encoded-der-request}`
6. Verify production settings (HTTPS, security headers, auth mode, and signer cert expiry monitoring in dashboard).
