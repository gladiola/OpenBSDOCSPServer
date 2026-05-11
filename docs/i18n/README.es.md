# OpenBSDOCSPServer (Español)

## Qué hace este programa
OpenBSDOCSPServer es un respondedor OCSP en ASP.NET Core para operaciones PKI tipo OpenBSD.

Funciones principales:
- Responde OCSP por `POST /ocsp` y `GET /ocsp/{base64url-request}`.
- Firma respuestas OCSP con credenciales desde PFX o PEM.
- Incluye panel administrativo autenticado para revisar certificados, revocar/restaurar y agregar notas.
- Importa estados desde `index.txt` de OpenSSL, archivos de texto y sincronización proxy OCSP.
- Guarda estados de certificados en SQLite.
- Soporta endurecimiento de seguridad: cabeceras estrictas, mTLS opcional y autenticación Entra ID opcional.

## RFC aplicables
- RFC 6960 — OCSP.
- RFC 5019 — Perfil OCSP ligero.
- RFC 8954 — Extensión Nonce de OCSP.

## Instalación
1. Instale .NET SDK 9.0.
2. Clone el repositorio.
3. Compile: `dotnet build OcspServer/OcspServer.csproj`
4. Pruebas (recomendado): `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. Ejecute: `dotnet run --project OcspServer/OcspServer.csproj`

## Configuración
Edite `OcspServer/appsettings.json` (o variables de entorno/secrets):
- `FeatureFlags`
- `OcspServer`
- `AdminAuth`
- `AzureAd` (si usa Entra)
- `Ingestion`

## Poner en operación
1. Configure credenciales de firma (PFX o PEM).
2. Configure autenticación de admin (hash PBKDF2 local o Entra ID).
3. Configure `Ingestion.DatabasePath` e inicie la aplicación.
4. En `/admin`, importe registros de certificados.
5. Apunte clientes OCSP a `POST /ocsp` o `GET /ocsp/{base64url-encoded-der-request}`.
6. Verifique HTTPS, cabeceras, modo de autenticación y vigencia del certificado firmante.
