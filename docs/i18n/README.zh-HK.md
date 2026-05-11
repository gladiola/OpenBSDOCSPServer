# OpenBSDOCSPServer（繁體中文・香港）

## 程式用途
OpenBSDOCSPServer 是以 ASP.NET Core 實作的 OCSP 回應器，適用於 OpenBSD 風格 PKI 環境。

主要功能：
- 提供 `POST /ocsp` 及 `GET /ocsp/{base64url-request}` OCSP 查詢。
- 以 PFX 或 PEM 憑證金鑰簽署 OCSP 回應。
- 提供已驗證的管理介面：檢視證書、吊銷/恢復、註記。
- 可從 OpenSSL `index.txt`、文字檔、OCSP Proxy 同步匯入狀態。
- 使用 SQLite 儲存證書狀態。
- 支援安全強化：安全標頭、可選 mTLS、可選 Entra ID 管理登入。

## 適用 RFC
- RFC 6960、RFC 5019、RFC 8954。

## 安裝
1. 安裝 .NET SDK 9.0。
2. 複製此儲存庫。
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## 設定
編輯 `OcspServer/appsettings.json`：`FeatureFlags`、`OcspServer`、`AdminAuth`、`AzureAd`、`Ingestion`。

## 上線配置步驟
1. 設定簽章憑證（PFX 或 PEM）。
2. 設定管理驗證（本機 PBKDF2 或 Entra ID）。
3. 設定 `Ingestion.DatabasePath` 並啟動服務。
4. 透過 `/admin` 匯入證書資料。
5. OCSP 客戶端指向 `POST /ocsp` 或 `GET /ocsp/{base64url-encoded-der-request}`。
6. 檢查 HTTPS、安全標頭、驗證模式及簽章證書到期日。
