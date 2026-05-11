# OpenBSDOCSPServer (עברית)

## מה התוכנית עושה
OpenBSDOCSPServer הוא שרת OCSP מבוסס ASP.NET Core עבור תפעול PKI בסגנון OpenBSD.

יכולות עיקריות:
- מתן תגובות OCSP דרך `POST /ocsp` ו-`GET /ocsp/{base64url-request}`.
- חתימה על תגובות עם אישורי PFX או PEM.
- ממשק ניהול מאומת לסקירה, revoke/reinstate והערות.
- ייבוא נתונים מ-OpenSSL `index.txt`, מקובץ טקסט ומ-OCSP proxy sync.
- שמירת מצב תעודות ב-SQLite.
- הקשחת אבטחה: security headers, mTLS אופציונלי, Entra ID אופציונלי.

## RFC רלוונטיים
- RFC 6960, RFC 5019, RFC 8954.

## התקנה
1. התקן .NET SDK 9.0.
2. בצע clone למאגר.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## תצורה
ערוך את `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## הפעלה בפועל
1. הגדר signing credentials (PFX או PEM).
2. הגדר admin auth (PBKDF2 מקומי או Entra ID).
3. הגדר `Ingestion.DatabasePath` והפעל את האפליקציה.
4. ייבא רשומות דרך `/admin`.
5. הפנה לקוחות ל-`POST /ocsp` או `GET /ocsp/{base64url-encoded-der-request}`.
6. בדוק HTTPS, security headers, מצב auth ותוקף signer cert.
