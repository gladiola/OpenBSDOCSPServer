# OpenBSDOCSPServer (Eesti)

## Mida see programm teeb
OpenBSDOCSPServer on ASP.NET Core OCSP-vastaja OpenBSD-stiilis PKI tööks.

Põhifunktsioonid:
- OCSP vastused `POST /ocsp` ja `GET /ocsp/{base64url-request}` kaudu.
- Vastuste allkirjastamine PFX või PEM võtmetega.
- Autenditud admin UI sertifikaatide vaatamiseks, revoke/reinstate ja märkmete jaoks.
- Import OpenSSL `index.txt`, tekstifaili ja OCSP proxy sync kaudu.
- Sertifikaadi olekute salvestus SQLite andmebaasis.
- Turvakarmistus: security headers, valikuline mTLS, valikuline Entra ID.

## Rakenduvad RFC-d
- RFC 6960, RFC 5019, RFC 8954.

## Paigaldus
1. Paigalda .NET SDK 9.0.
2. Klooni hoidla.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Seadistamine
Muuda `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Kasutuselevõtt
1. Seadista allkirjastamise andmed (PFX või PEM).
2. Seadista admin auth (kohalik PBKDF2 või Entra ID).
3. Sea `Ingestion.DatabasePath` ja käivita rakendus.
4. Impordi kirjed `/admin` kaudu.
5. Suuna kliendid `POST /ocsp` või `GET /ocsp/{base64url-encoded-der-request}`.
6. Kontrolli HTTPS-i, security headers, auth mode’i ja signer cert expiry’t.
