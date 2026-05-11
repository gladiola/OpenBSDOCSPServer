# OpenBSDOCSPServer (Yorùbá)

## Ohun tí eto yìí ń ṣe
OpenBSDOCSPServer jẹ́ OCSP responder lori ASP.NET Core fún iṣẹ́ PKI irú OpenBSD.

Àwọn àǹfààní pàtàkì:
- Ó ń dáhùn OCSP ní `POST /ocsp` àti `GET /ocsp/{base64url-request}`.
- Ó ń fọwọ́si ìdáhùn pẹ̀lú PFX tàbí PEM credentials.
- Ó ní admin UI tó ni ìmúdájú fún ìwòye, revoke/reinstate, àti àkọsílẹ̀.
- Ó ń wọlé data láti OpenSSL `index.txt`, text file, àti OCSP proxy sync.
- Ó ń fipamọ́ status sí SQLite.
- Aabo líle: security headers, optional mTLS, optional Entra ID.

## RFC tó bá mu
- RFC 6960, RFC 5019, RFC 8954.

## Fifi sori
1. Fi .NET SDK 9.0 sori ẹrọ.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Ìtọ́kasí (configuration)
Ṣatúnṣe `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Bí a ṣe máa ṣiṣẹ́
1. Ṣètò signing credentials (PFX tàbí PEM).
2. Ṣètò admin auth (PBKDF2 abẹ́lé tàbí Entra ID).
3. Ṣètò `Ingestion.DatabasePath` kí o sì bẹ̀rẹ̀ app.
4. Kó certificate records wọlé ní `/admin`.
5. Tọ́ clients sí `POST /ocsp` tàbí `GET /ocsp/{base64url-encoded-der-request}`.
6. Ṣàyẹ̀wò HTTPS, security headers, auth mode, àti ọjọ́ ipari signer cert.
