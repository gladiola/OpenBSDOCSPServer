# OpenBSDOCSPServer (Français)

## Ce que fait ce programme
OpenBSDOCSPServer est un répondeur OCSP ASP.NET Core pour des opérations PKI de type OpenBSD.

Fonctionnalités principales:
- Réponses OCSP via `POST /ocsp` et `GET /ocsp/{base64url-request}`.
- Signature des réponses OCSP avec des identités PFX ou PEM.
- Interface d’administration authentifiée (consultation, révocation/rétablissement, notes).
- Import des statuts via `index.txt` OpenSSL, fichier texte, et synchronisation proxy OCSP.
- Stockage SQLite des statuts de certificats.
- Durcissement sécurité: en-têtes stricts, mTLS optionnel, Entra ID optionnel.

## RFC applicables
- RFC 6960 — OCSP.
- RFC 5019 — Profil OCSP léger.
- RFC 8954 — Extension Nonce OCSP.

## Installation
1. Installer .NET SDK 9.0.
2. Cloner le dépôt.
3. Build: `dotnet build OcspServer/OcspServer.csproj`
4. Tests (recommandé): `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. Exécution: `dotnet run --project OcspServer/OcspServer.csproj`

## Configuration
Configurer `OcspServer/appsettings.json` (ou variables d’environnement/secrets):
- `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd` (si activé), `Ingestion`.

## Mise en service
1. Configurer les identifiants de signature (PFX ou PEM).
2. Configurer l’auth admin (PBKDF2 local ou Entra ID).
3. Définir `Ingestion.DatabasePath` puis démarrer l’application.
4. Importer les enregistrements via `/admin`.
5. Diriger les clients vers `POST /ocsp` ou `GET /ocsp/{base64url-encoded-der-request}`.
6. Vérifier HTTPS, en-têtes, mode d’authentification et expiration du certif de signature.
