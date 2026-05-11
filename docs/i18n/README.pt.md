# OpenBSDOCSPServer (Português)

## O que este programa faz
OpenBSDOCSPServer é um respondedor OCSP em ASP.NET Core para operações de PKI no estilo OpenBSD.

Principais recursos:
- Responde OCSP em `POST /ocsp` e `GET /ocsp/{base64url-request}`.
- Assina respostas com credenciais PFX ou PEM.
- UI administrativa autenticada para revisar, revogar/reintegrar e anotar certificados.
- Importa dados de `index.txt` do OpenSSL, arquivo texto e sincronização por proxy OCSP.
- Armazena status em SQLite.
- Inclui endurecimento de segurança: cabeçalhos, mTLS opcional e Entra ID opcional.

## RFCs aplicáveis
- RFC 6960, RFC 5019, RFC 8954.

## Instalação
1. Instale .NET SDK 9.0.
2. Clone o repositório.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Configuração
Edite `OcspServer/appsettings.json`:
- `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd` (quando aplicável), `Ingestion`.

## Colocar em operação
1. Configure credenciais de assinatura (PFX ou PEM).
2. Configure autenticação admin (PBKDF2 local ou Entra ID).
3. Ajuste `Ingestion.DatabasePath` e inicie o serviço.
4. Importe dados em `/admin`.
5. Aponte clientes para `POST /ocsp` ou `GET /ocsp/{base64url-encoded-der-request}`.
6. Valide HTTPS, cabeçalhos, autenticação e validade do certificado assinante.
