# OpenBSDOCSPServer (Русский)

## Назначение программы
OpenBSDOCSPServer — OCSP-респондер на ASP.NET Core для PKI-сценариев в стиле OpenBSD.

Основные возможности:
- Ответы OCSP через `POST /ocsp` и `GET /ocsp/{base64url-request}`.
- Подпись OCSP-ответов ключами из PFX или PEM.
- Защищённый админ-интерфейс для просмотра, отзыва/восстановления и заметок.
- Импорт статусов из OpenSSL `index.txt`, текстовых файлов и live OCSP proxy sync.
- Хранение статусов в SQLite.
- Усиление безопасности: security headers, optional mTLS, optional Entra ID.

## Применимые RFC
- RFC 6960, RFC 5019, RFC 8954.

## Установка
1. Установите .NET SDK 9.0.
2. Клонируйте репозиторий.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Конфигурация
Настройте `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Ввод в эксплуатацию
1. Настройте подписывающие данные (PFX или PEM).
2. Настройте админ-аутентификацию (локальный PBKDF2 или Entra ID).
3. Укажите `Ingestion.DatabasePath` и запустите приложение.
4. Импортируйте записи через `/admin`.
5. Направьте клиентов на `POST /ocsp` или `GET /ocsp/{base64url-encoded-der-request}`.
6. Проверьте HTTPS, заголовки, режим аутентификации и срок действия сертификата подписи.
