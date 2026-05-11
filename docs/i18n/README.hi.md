# OpenBSDOCSPServer (हिन्दी)

## यह प्रोग्राम क्या करता है
OpenBSDOCSPServer एक ASP.NET Core आधारित OCSP responder है, जो OpenBSD शैली PKI संचालन के लिए बनाया गया है।

मुख्य विशेषताएँ:
- `POST /ocsp` और `GET /ocsp/{base64url-request}` पर OCSP उत्तर देता है।
- PFX या PEM क्रेडेंशियल से OCSP प्रतिक्रियाएँ साइन करता है।
- प्रमाणित admin UI: सर्टिफिकेट देखना, revoke/reinstate करना, नोट्स जोड़ना।
- OpenSSL `index.txt`, text file, और live OCSP proxy sync से इम्पोर्ट।
- SQLite में सर्टिफिकेट स्टेटस स्टोर।
- सुरक्षा फीचर: security headers, वैकल्पिक mTLS, वैकल्पिक Entra ID auth।

## लागू RFC
- RFC 6960, RFC 5019, RFC 8954.

## इंस्टॉलेशन
1. .NET SDK 9.0 इंस्टॉल करें।
2. Repository clone करें।
3. `dotnet build OcspServer/OcspServer.csproj`
4. `dotnet test OcspServer.Tests/OcspServer.Tests.csproj`
5. `dotnet run --project OcspServer/OcspServer.csproj`

## कॉन्फ़िगरेशन
`OcspServer/appsettings.json` में `FeatureFlags`, `OcspServer`, `AdminAuth`, `AzureAd`, `Ingestion` सेट करें।

## संचालन के लिए कॉन्फ़िगर करना
1. Signing credentials (PFX या PEM) सेट करें।
2. Admin authentication (लोकल PBKDF2 या Entra ID) सेट करें।
3. `Ingestion.DatabasePath` सेट करके ऐप चालू करें।
4. `/admin` से certificate records इम्पोर्ट करें।
5. OCSP clients को `POST /ocsp` या `GET /ocsp/{base64url-encoded-der-request}` पर भेजें।
6. HTTPS, headers, auth mode और signer certificate expiry जाँचें।
