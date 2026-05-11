# OpenBSDOCSPServer（日本語）

## このプログラムの概要
OpenBSDOCSPServer は、OpenBSD 方式の PKI 運用向け ASP.NET Core 製 OCSP レスポンダーです。

主な機能:
- `POST /ocsp` と `GET /ocsp/{base64url-request}` で OCSP 応答を提供。
- PFX または PEM の資格情報で OCSP 応答に署名。
- 認証付き管理 UI（証明書確認、失効/復帰、メモ）。
- OpenSSL `index.txt`、テキストファイル、OCSP proxy sync から状態を取り込み。
- SQLite に証明書状態を保存。
- セキュリティ強化（ヘッダー、任意 mTLS、任意 Entra ID 認証）。

## 関連 RFC
- RFC 6960、RFC 5019、RFC 8954。

## インストール
1. .NET SDK 9.0 をインストール。
2. リポジトリをクローン。
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## 設定
`OcspServer/appsettings.json` の `FeatureFlags`、`OcspServer`、`AdminAuth`、`AzureAd`、`Ingestion` を設定。

## 運用開始手順
1. 署名資格情報（PFX または PEM）を設定。
2. 管理者認証（ローカル PBKDF2 または Entra ID）を設定。
3. `Ingestion.DatabasePath` を設定して起動。
4. `/admin` から証明書データをインポート。
5. クライアントを `POST /ocsp` または `GET /ocsp/{base64url-encoded-der-request}` に向ける。
6. HTTPS、セキュリティヘッダー、認証方式、署名証明書の有効期限を確認。
