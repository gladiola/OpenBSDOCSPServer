# OpenBSDOCSPServer（简体中文）

## 程序说明
OpenBSDOCSPServer 是一个基于 ASP.NET Core 的 OCSP 响应服务，面向 OpenBSD 风格 PKI 场景。

主要功能：
- 通过 `POST /ocsp` 和 `GET /ocsp/{base64url-request}` 提供 OCSP 响应。
- 使用 PFX 或 PEM 凭据对响应进行签名。
- 提供已认证的管理界面（查看证书、吊销/恢复、备注）。
- 支持从 OpenSSL `index.txt`、文本文件、OCSP 代理同步导入状态。
- 使用 SQLite 存储证书状态。
- 支持安全加固：安全头、可选 mTLS、可选 Entra ID 管理认证。

## 相关 RFC
- RFC 6960、RFC 5019、RFC 8954。

## 安装
1. 安装 .NET SDK 9.0。
2. 克隆仓库。
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## 配置
编辑 `OcspServer/appsettings.json`：`FeatureFlags`、`OcspServer`、`AdminAuth`、`AzureAd`、`Ingestion`。

## 投入运行
1. 配置签名凭据（PFX 或 PEM）。
2. 配置管理认证（本地 PBKDF2 或 Entra ID）。
3. 设置 `Ingestion.DatabasePath` 并启动应用。
4. 在 `/admin` 导入证书状态记录。
5. 将 OCSP 客户端指向 `POST /ocsp` 或 `GET /ocsp/{base64url-encoded-der-request}`。
6. 检查 HTTPS、安全头、认证模式和签名证书到期时间。
