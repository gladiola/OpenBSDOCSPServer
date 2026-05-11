# OpenBSDOCSPServer (አማርኛ)

## ይህ ፕሮግራም ምን ያደርጋል
OpenBSDOCSPServer ለ OpenBSD አይነት PKI አሰራር የተዘጋጀ ASP.NET Core የ OCSP መልስ አገልጋይ ነው።

ዋና ባህሪያት:
- OCSP መልሶችን በ `POST /ocsp` እና `GET /ocsp/{base64url-request}` ይሰጣል።
- መልሶችን በ PFX ወይም PEM ማረጋገጫ ይፈርማል።
- የተጠበቀ admin UI ያቀርባል (እይታ፣ revoke/reinstate፣ ማስታወሻ).
- ከ OpenSSL `index.txt`፣ text file እና OCSP proxy sync ውሂብ ያስመጣል።
- ውሂብን በ SQLite ይቆጥባል።
- የደህንነት ጠንካራነት: security headers, optional mTLS, optional Entra ID.

## የሚመለከቱ RFC ማጣቀሻዎች
- RFC 6960, RFC 5019, RFC 8954.

## መጫን
1. .NET SDK 9.0 ይጫኑ።
2. ሪፖዚቶሪውን clone ያድርጉ።
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## ማዋቀር
`OcspServer/appsettings.json` ውስጥ `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion` ያዘጋጁ።

## ወደ ስራ ማስገባት
1. የፊርማ መረጃዎችን (PFX ወይም PEM) ያቀናብሩ።
2. admin auth (local PBKDF2 ወይም Entra ID) ያዘጋጁ።
3. `Ingestion.DatabasePath` ያዘጋጁ እና app ያስጀምሩ።
4. በ `/admin` የcertificate records ያስመጡ።
5. clients ወደ `POST /ocsp` ወይም `GET /ocsp/{base64url-encoded-der-request}` ያመሩ።
6. HTTPS, security headers, auth mode, signer cert expiry ያረጋግጡ።
