using OcspServer.Controllers;
using OcspServer.Services.Ocsp;

namespace OcspServer.Tests;

/// <summary>
/// Tests for the ApplicationBuilderExtensions security header logic,
/// specifically the OCSP path exemption.
/// </summary>
public class SecurityHeadersTests
{
    // The path check logic is embedded in the middleware delegate.
    // We test the path-based branching rule directly by mirroring it here.

    private static bool IsOcspPath(string path) =>
        path.StartsWith("/ocsp", StringComparison.OrdinalIgnoreCase);

    [Theory]
    [InlineData("/ocsp", true)]
    [InlineData("/OCSP", true)]
    [InlineData("/ocsp/", true)]
    [InlineData("/ocsp/MEQC...", true)]
    [InlineData("/", false)]
    [InlineData("/admin", false)]
    [InlineData("/admin/certificates", false)]
    [InlineData("/account/login", false)]
    [InlineData("/Home/Index", false)]
    public void OcspPathDetection_IsCorrect(string path, bool expected) =>
        Assert.Equal(expected, IsOcspPath(path));
}

/// <summary>
/// Tests for the OcspResponseStatus constants matching RFC 6960 §4.2.1 values.
/// </summary>
public class OcspResponseStatusTests
{
    [Fact]
    public void OcspResponseStatus_MatchesRfc6960Values()
    {
        // RFC 6960 §4.2.1 response status values
        Assert.Equal(0, OcspResponseStatus.Successful);
        Assert.Equal(1, OcspResponseStatus.MalformedRequest);
        Assert.Equal(2, OcspResponseStatus.InternalError);
        Assert.Equal(3, OcspResponseStatus.TryLater);
        Assert.Equal(5, OcspResponseStatus.SigRequired);
        Assert.Equal(6, OcspResponseStatus.Unauthorized);
    }
}
