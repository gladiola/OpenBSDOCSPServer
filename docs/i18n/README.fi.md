# OpenBSDOCSPServer (Suomi)

## Mitä ohjelma tekee
OpenBSDOCSPServer on ASP.NET Core -pohjainen OCSP-vastaaja OpenBSD-tyyliseen PKI-käyttöön.

Pääominaisuudet:
- OCSP-vastaukset reiteillä `POST /ocsp` ja `GET /ocsp/{base64url-request}`.
- Vastaukset allekirjoitetaan PFX- tai PEM-tunnuksilla.
- Todennettu admin UI (tarkastelu, revoke/reinstate, muistiinpanot).
- Tuonti OpenSSL `index.txt` -tiedostosta, tekstitiedostosta ja OCSP proxy syncistä.
- Sertifikaattien tila tallennetaan SQLiteen.
- Turvakovennus: security headers, valinnainen mTLS, valinnainen Entra ID.

## Soveltuvat RFC:t
- RFC 6960, RFC 5019, RFC 8954.

## Asennus
1. Asenna .NET SDK 9.0.
2. Kloonaa repositorio.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigurointi
Muokkaa `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Käyttöönotto
1. Aseta allekirjoituscredentialit (PFX tai PEM).
2. Aseta admin-auth (paikallinen PBKDF2 tai Entra ID).
3. Aseta `Ingestion.DatabasePath` ja käynnistä sovellus.
4. Tuo sertifikaattitiedot `/admin`-näkymässä.
5. Osoita asiakkaat `POST /ocsp` tai `GET /ocsp/{base64url-encoded-der-request}`.
6. Tarkista HTTPS, security headers, auth mode ja signer cert expiry.
