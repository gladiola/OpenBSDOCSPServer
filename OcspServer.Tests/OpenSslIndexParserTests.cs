using OcspServer.Services.Ingestion;

namespace OcspServer.Tests;

public class OpenSslIndexParserTests
{
    private static ILogger<OpenSslIndexParser> Logger() =>
        Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenSslIndexParser>.Instance;

    private OpenSslIndexParser MakeParser(
        string issuerDn = "CN=Test CA",
        string sha1 = "AABBCCDDEEFF00112233445566778899AABBCCDD",
        string sha256 = "") =>
        new OpenSslIndexParser(Logger(), issuerDn, sha1, sha256);

    // ── Date parsing ─────────────────────────────────────────────────────────

    [Fact]
    public void ParseOpenSslDate_ValidDate_ReturnsParsedUtc()
    {
        var dt = OpenSslIndexParser.ParseOpenSslDate("250101120000Z");
        Assert.NotNull(dt);
        Assert.Equal(2025, dt!.Value.Year);
        Assert.Equal(1, dt.Value.Month);
        Assert.Equal(1, dt.Value.Day);
        Assert.Equal(12, dt.Value.Hour);
        Assert.Equal(DateTimeKind.Utc, dt.Value.Kind);
    }

    [Fact]
    public void ParseOpenSslDate_YearAbove50_Returns1900Century()
    {
        var dt = OpenSslIndexParser.ParseOpenSslDate("991231235959Z");
        Assert.Equal(1999, dt!.Value.Year);
    }

    [Fact]
    public void ParseOpenSslDate_Empty_ReturnsNull() =>
        Assert.Null(OpenSslIndexParser.ParseOpenSslDate(""));

    [Fact]
    public void ParseOpenSslDate_Garbage_ReturnsNull() =>
        Assert.Null(OpenSslIndexParser.ParseOpenSslDate("notadate"));

    // ── Reason parsing ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("keyCompromise", Models.RevocationReason.KeyCompromise)]
    [InlineData("CACompromise", Models.RevocationReason.CACompromise)]
    [InlineData("cessationOfOperation", Models.RevocationReason.CessationOfOperation)]
    [InlineData("certificateHold", Models.RevocationReason.CertificateHold)]
    [InlineData("unknownReason", Models.RevocationReason.Unspecified)]
    public void ParseReason_KnownReasons_MapsCorrectly(string input, Models.RevocationReason expected) =>
        Assert.Equal(expected, OpenSslIndexParser.ParseReason(input));

    // ── Full line parsing ─────────────────────────────────────────────────────

    [Fact]
    public void ParseText_ValidLine_ReturnsGoodRecord()
    {
        const string line = "V\t300101000000Z\t\t0A1B2C\tunknown\t/CN=test";
        var parser = MakeParser();
        var results = parser.ParseText(line).ToList();

        Assert.Single(results);
        var (rec, err) = results[0];
        Assert.Null(err);
        Assert.Equal("A1B2C", rec.SerialNumber);   // leading zero stripped
        Assert.Equal(Models.CertificateStatus.Good, rec.Status);
        Assert.Equal("/CN=test", rec.SubjectDN);
    }

    [Fact]
    public void ParseText_RevokedLine_ReturnsRevokedRecord()
    {
        const string line = "R\t300101000000Z\t240601120000Z,keyCompromise\t0001\tunknown\t/CN=rev";
        var parser = MakeParser();
        var results = parser.ParseText(line).ToList();

        Assert.Single(results);
        var (rec, err) = results[0];
        Assert.Null(err);
        Assert.Equal(Models.CertificateStatus.Revoked, rec.Status);
        Assert.Equal(Models.RevocationReason.KeyCompromise, rec.RevocationReason);
        Assert.NotNull(rec.RevocationDate);
        Assert.Equal(2024, rec.RevocationDate!.Value.Year);
    }

    [Fact]
    public void ParseText_ExpiredLine_TreatedAsGood()
    {
        const string line = "E\t200101000000Z\t\t00FF\tunknown\t/CN=exp";
        var (rec, err) = MakeParser().ParseText(line).First();
        Assert.Null(err);
        Assert.Equal(Models.CertificateStatus.Good, rec.Status);
    }

    [Fact]
    public void ParseText_TooFewFields_ReturnsError()
    {
        var (_, err) = MakeParser().ParseText("V\t300101000000Z").First();
        Assert.NotNull(err);
    }

    [Fact]
    public void ParseText_UnknownFlag_ReturnsError()
    {
        const string line = "X\t300101000000Z\t\t00FF\tunknown\t/CN=x";
        var (_, err) = MakeParser().ParseText(line).First();
        Assert.NotNull(err);
    }

    [Fact]
    public void ParseText_CommentAndBlankLines_AreSkipped()
    {
        const string input = """
            # This is a comment
            
            V	300101000000Z		0001	unknown	/CN=one
            """;
        var results = MakeParser().ParseText(input).ToList();
        Assert.Single(results);
    }

    [Fact]
    public void ParseText_MultipleRecords_AllParsed()
    {
        const string input = """
            V	300101000000Z		0001	unknown	/CN=one
            R	300101000000Z	240101000000Z	0002	unknown	/CN=two
            V	300101000000Z		0003	unknown	/CN=three
            """;
        var results = MakeParser().ParseText(input).ToList();
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Null(r.Error));
    }

    [Fact]
    public void ParseText_IssuerHashesArePopulated()
    {
        const string line = "V\t300101000000Z\t\t0001\tunknown\t/CN=x";
        var (rec, _) = MakeParser(sha1: "AABBCCDD").ParseText(line).First();
        Assert.Equal("AABBCCDD", rec.IssuerKeyHashSha1);
    }

    [Fact]
    public void ParseText_SerialLeadingZeroStripped()
    {
        const string line = "V\t300101000000Z\t\t000ABC\tunknown\t/CN=x";
        var (rec, _) = MakeParser().ParseText(line).First();
        Assert.Equal("ABC", rec.SerialNumber);
    }
}
