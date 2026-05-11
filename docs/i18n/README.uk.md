# OpenBSDOCSPServer (Українська)

## Що робить ця програма
OpenBSDOCSPServer — це OCSP-респондент на ASP.NET Core для PKI-сценаріїв у стилі OpenBSD.

Основні можливості:
- Відповіді OCSP через `POST /ocsp` і `GET /ocsp/{base64url-request}`.
- Підпис відповідей за допомогою PFX або PEM.
- Захищений admin UI для перегляду, revoke/reinstate та нотаток.
- Імпорт із OpenSSL `index.txt`, текстових файлів і OCSP proxy sync.
- Зберігання станів у SQLite.
- Підсилення безпеки: security headers, optional mTLS, optional Entra ID.

## Застосовні RFC
- RFC 6960, RFC 5019, RFC 8954.

## Встановлення
1. Встановіть .NET SDK 9.0.
2. Клонуйте репозиторій.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Налаштування
Змініть `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Введення в експлуатацію
1. Налаштуйте підписні дані (PFX або PEM).
2. Налаштуйте admin auth (локальний PBKDF2 або Entra ID).
3. Вкажіть `Ingestion.DatabasePath` і запустіть застосунок.
4. Імпортуйте записи через `/admin`.
5. Спрямуйте клієнтів на `POST /ocsp` або `GET /ocsp/{base64url-encoded-der-request}`.
6. Перевірте HTTPS, security headers, режим auth і строк дії signer cert.
