# OpenBSDOCSPServer (한국어)

## 프로그램 개요
OpenBSDOCSPServer는 OpenBSD 스타일 PKI 운영을 위한 ASP.NET Core OCSP 응답 서버입니다.

주요 기능:
- `POST /ocsp`, `GET /ocsp/{base64url-request}` 지원.
- PFX/PEM 자격 증명으로 OCSP 응답 서명.
- 인증된 관리자 UI(조회, 폐지/복구, 메모).
- OpenSSL `index.txt`, 텍스트 파일, OCSP 프록시 동기화 가져오기.
- SQLite 기반 상태 저장.
- 보안 강화(헤더, 선택적 mTLS, 선택적 Entra ID).

## 관련 RFC
- RFC 6960, RFC 5019, RFC 8954.

## 설치
1. .NET SDK 9.0 설치.
2. 저장소 클론.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## 구성
`OcspServer/appsettings.json`에서 `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion` 설정.

## 운영 설정
1. 서명 자격(PFX 또는 PEM) 설정.
2. 관리자 인증(PBKDF2 로컬 또는 Entra ID) 설정.
3. `Ingestion.DatabasePath` 설정 후 실행.
4. `/admin`에서 인증서 상태 데이터 가져오기.
5. 클라이언트를 `POST /ocsp` 또는 `GET /ocsp/{base64url-encoded-der-request}`로 연결.
6. HTTPS/보안 헤더/인증 모드/서명 인증서 만료 확인.
