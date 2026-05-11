# OpenBSDOCSPServer (ไทย)

## โปรแกรมนี้ทำอะไร
OpenBSDOCSPServer คือ OCSP responder บน ASP.NET Core สำหรับงาน PKI แนวทาง OpenBSD

ความสามารถหลัก:
- ให้บริการ OCSP ผ่าน `POST /ocsp` และ `GET /ocsp/{base64url-request}`
- ลงนามคำตอบด้วยข้อมูลรับรองแบบ PFX หรือ PEM
- มีหน้า admin UI ที่ยืนยันตัวตนสำหรับดูใบรับรอง revoke/reinstate และบันทึกหมายเหตุ
- นำเข้าข้อมูลจาก OpenSSL `index.txt`, ไฟล์ข้อความ และ OCSP proxy sync
- เก็บสถานะใบรับรองใน SQLite
- รองรับการ hardening: security headers, optional mTLS, optional Entra ID

## RFC ที่เกี่ยวข้อง
- RFC 6960, RFC 5019, RFC 8954.

## การติดตั้ง
1. ติดตั้ง .NET SDK 9.0
2. โคลน repository
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## การตั้งค่า
แก้ไข `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`

## การตั้งค่าเพื่อใช้งานจริง
1. ตั้งค่า signing credentials (PFX หรือ PEM)
2. ตั้งค่า admin auth (PBKDF2 ภายใน หรือ Entra ID)
3. ตั้งค่า `Ingestion.DatabasePath` แล้วเริ่มแอป
4. นำเข้าข้อมูลใบรับรองที่ `/admin`
5. ชี้ client ไปที่ `POST /ocsp` หรือ `GET /ocsp/{base64url-encoded-der-request}`
6. ตรวจสอบ HTTPS, security headers, โหมด auth และวันหมดอายุ signer cert
