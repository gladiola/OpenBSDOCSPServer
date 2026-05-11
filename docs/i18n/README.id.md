# OpenBSDOCSPServer (Bahasa Indonesia)

## Fungsi program
OpenBSDOCSPServer adalah OCSP responder berbasis ASP.NET Core untuk operasi PKI gaya OpenBSD.

Fitur utama:
- Menyajikan respons OCSP pada `POST /ocsp` dan `GET /ocsp/{base64url-request}`.
- Menandatangani respons dengan kredensial PFX atau PEM.
- Admin UI terautentikasi untuk melihat sertifikat, revoke/reinstate, dan catatan.
- Impor data dari OpenSSL `index.txt`, file teks, dan OCSP proxy sync.
- Menyimpan status sertifikat di SQLite.
- Mendukung hardening keamanan: security headers, mTLS opsional, Entra ID opsional.

## RFC terkait
- RFC 6960, RFC 5019, RFC 8954.

## Instalasi
1. Instal .NET SDK 9.0.
2. Clone repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Konfigurasi
Edit `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Konfigurasi agar beroperasi
1. Atur kredensial penandatanganan (PFX atau PEM).
2. Atur admin auth (PBKDF2 lokal atau Entra ID).
3. Atur `Ingestion.DatabasePath` lalu jalankan aplikasi.
4. Impor data sertifikat melalui `/admin`.
5. Arahkan klien ke `POST /ocsp` atau `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifikasi HTTPS, security headers, mode auth, dan masa berlaku signer cert.
