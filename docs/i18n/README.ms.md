# OpenBSDOCSPServer (Bahasa Melayu)

## Apa fungsi program ini
OpenBSDOCSPServer ialah OCSP responder berasaskan ASP.NET Core untuk operasi PKI gaya OpenBSD.

Ciri utama:
- Menyediakan respons OCSP melalui `POST /ocsp` dan `GET /ocsp/{base64url-request}`.
- Menandatangani respons menggunakan kelayakan PFX atau PEM.
- Admin UI berautentikasi untuk semakan, revoke/reinstate, dan nota.
- Import data daripada OpenSSL `index.txt`, fail teks, dan OCSP proxy sync.
- Menyimpan status sijil dalam SQLite.
- Menyokong hardening keselamatan: security headers, mTLS pilihan, Entra ID pilihan.

## RFC berkaitan
- RFC 6960, RFC 5019, RFC 8954.

## Pemasangan
1. Pasang .NET SDK 9.0.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigurasi
Edit `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Konfigurasi untuk operasi
1. Tetapkan signing credentials (PFX atau PEM).
2. Tetapkan admin auth (PBKDF2 tempatan atau Entra ID).
3. Tetapkan `Ingestion.DatabasePath` dan jalankan aplikasi.
4. Import rekod sijil melalui `/admin`.
5. Halakan klien ke `POST /ocsp` atau `GET /ocsp/{base64url-encoded-der-request}`.
6. Sahkan HTTPS, security headers, auth mode, dan tarikh luput signer cert.
