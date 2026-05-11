# OpenBSDOCSPServer (العربية)

## ماذا يفعل هذا البرنامج
OpenBSDOCSPServer هو خادم OCSP مبني على ASP.NET Core لعمليات PKI بأسلوب OpenBSD.

الميزات الأساسية:
- تقديم استجابات OCSP عبر `POST /ocsp` و `GET /ocsp/{base64url-request}`.
- توقيع الاستجابات باستخدام بيانات اعتماد من ملفات PFX أو PEM.
- واجهة إدارة موثّقة لمراجعة الشهادات وإبطالها/إعادتها وإضافة الملاحظات.
- استيراد بيانات الحالة من `index.txt` الخاص بـ OpenSSL، ومن ملفات نصية، ومن مزامنة OCSP proxy.
- تخزين الحالة في SQLite.
- دعم تقوية الأمان: رؤوس أمان، mTLS اختياري، وEntra ID اختياري.

## مراجع RFC
- RFC 6960، RFC 5019، RFC 8954.

## التثبيت
1. ثبّت .NET SDK 9.0.
2. انسخ المستودع.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## الإعداد
عدّل `OcspServer/appsettings.json` للأقسام: `FeatureFlags` و`OcspServer` و`AdminAuth` و`AzureAd` و`Ingestion`.

## التشغيل الفعلي
1. جهّز بيانات التوقيع (PFX أو PEM).
2. اضبط مصادقة المشرف (PBKDF2 محلي أو Entra ID).
3. اضبط `Ingestion.DatabasePath` وشغّل التطبيق.
4. استورد سجلات الشهادات من `/admin`.
5. وجّه عملاء OCSP إلى `POST /ocsp` أو `GET /ocsp/{base64url-encoded-der-request}`.
6. تحقّق من HTTPS والرؤوس الأمنية ووضع المصادقة وانتهاء شهادة التوقيع.
