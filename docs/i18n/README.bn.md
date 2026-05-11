# OpenBSDOCSPServer (বাংলা)

## এই প্রোগ্রাম কী করে
OpenBSDOCSPServer হলো OpenBSD-ধরনের PKI পরিচালনার জন্য ASP.NET Core ভিত্তিক OCSP responder।

মূল ফিচার:
- `POST /ocsp` এবং `GET /ocsp/{base64url-request}` এ OCSP রেসপন্স দেয়।
- PFX বা PEM credentials দিয়ে রেসপন্স সাইন করে।
- প্রমাণীকৃত admin UI (সার্টিফিকেট দেখা, revoke/reinstate, নোট যোগ)।
- OpenSSL `index.txt`, text file, এবং OCSP proxy sync থেকে ইমপোর্ট।
- SQLite-এ সার্টিফিকেট স্ট্যাটাস সংরক্ষণ।
- নিরাপত্তা: security headers, optional mTLS, optional Entra ID auth।

## প্রযোজ্য RFC
- RFC 6960, RFC 5019, RFC 8954.

## ইনস্টলেশন
1. .NET SDK 9.0 ইনস্টল করুন।
2. Repository clone করুন।
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## কনফিগারেশন
`OcspServer/appsettings.json` এ `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion` সেট করুন।

## অপারেশনে চালু করা
1. Signing credentials (PFX বা PEM) কনফিগার করুন।
2. Admin auth (লোকাল PBKDF2 বা Entra ID) সেট করুন।
3. `Ingestion.DatabasePath` সেট করে অ্যাপ চালু করুন।
4. `/admin` থেকে সার্টিফিকেট রেকর্ড ইমপোর্ট করুন।
5. OCSP clients কে `POST /ocsp` বা `GET /ocsp/{base64url-encoded-der-request}` এ নির্দেশ করুন।
6. HTTPS, security headers, auth mode, signer certificate expiry যাচাই করুন।
