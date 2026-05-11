# OpenBSDOCSPServer (Basa Jawa)

## Program iki kanggo apa
OpenBSDOCSPServer iku OCSP responder nganggo ASP.NET Core kanggo operasi PKI gaya OpenBSD.

Fitur utama:
- Nglayani OCSP ing `POST /ocsp` lan `GET /ocsp/{base64url-request}`.
- Nandhatangani respon nganggo kredensial PFX utawa PEM.
- Ana admin UI sing nganggo autentikasi kanggo mriksa, revoke/reinstate, lan cathetan.
- Nginpor data saka OpenSSL `index.txt`, file teks, lan OCSP proxy sync.
- Nyimpen status sertifikat ing SQLite.
- Ndukung hardening keamanan: security headers, mTLS opsional, Entra ID opsional.

## RFC sing cocog
- RFC 6960, RFC 5019, RFC 8954.

## Instalasi
1. Pasang .NET SDK 9.0.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigurasi
Owahi `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Supaya bisa mlaku
1. Setel signing credentials (PFX utawa PEM).
2. Setel admin auth (PBKDF2 lokal utawa Entra ID).
3. Setel `Ingestion.DatabasePath` banjur jalanke app.
4. Impor data ing `/admin`.
5. Arahna klien menyang `POST /ocsp` utawa `GET /ocsp/{base64url-encoded-der-request}`.
6. Priksa HTTPS, security headers, auth mode, lan expiry signer cert.
