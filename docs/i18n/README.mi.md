# OpenBSDOCSPServer (Te Reo Māori)

## He aha te mahi a tēnei hōtaka
He kaitautu OCSP a OpenBSDOCSPServer i runga i te ASP.NET Core mō ngā whakahaere PKI āhua OpenBSD.

Ngā āhuatanga matua:
- Ka tuku whakautu OCSP mā `POST /ocsp` me `GET /ocsp/{base64url-request}`.
- Ka haina i ngā whakautu mā ngā tohu PFX, PEM rānei.
- He atanga kaiwhakahaere kua motuhēhēhia mō te arotake, revoke/reinstate, me ngā tuhipoka.
- Ka kawemai raraunga mai i OpenSSL `index.txt`, kōnae kuputuhi, me te OCSP proxy sync.
- Ka penapena ki SQLite.
- Ka tautoko i te haumaru: security headers, mTLS kōwhiringa, Entra ID kōwhiringa.

## RFC hāngai
- RFC 6960, RFC 5019, RFC 8954.

## Tāutanga
1. Tāuta .NET SDK 9.0.
2. Tāruatia te repository.
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## Whirihoranga
Whakatikahia `OcspServer/appsettings.json`: `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion`.

## Whakahaere ki te mahi
1. Whirihora i ngā tohu haina (PFX, PEM rānei).
2. Whirihora i te admin auth (PBKDF2 ā-rohe, Entra ID rānei).
3. Tautuhi `Ingestion.DatabasePath` ka tīmata i te taupānga.
4. Kawemai raraunga mā `/admin`.
5. Aratakina ngā kiritaki ki `POST /ocsp` rānei `GET /ocsp/{base64url-encoded-der-request}`.
6. Arotake i te HTTPS, security headers, auth mode, me te paunga o te signer cert.
