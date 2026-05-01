using OcspServer.Services.Ingestion;
using OcspServer.Models;

namespace OcspServer.Tests;

public class TextFileImporterTests
{
    private static ILogger<TextFileImporter> Logger() =>
        Microsoft.Extensions.Logging.Abstractions.NullLogger<TextFileImporter>.Instance;

    private TextFileImporter MakeImporter() =>
        new TextFileImporter(Logger(), "CN=Test CA", "AABBCCDD");

    private IList<(CertificateRecord Record, string? Error)> Parse(string text)
    {
        var importer = MakeImporter();
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        return importer.Parse(ms).ToList();
    }

    [Fact]
    public void Parse_GoodLine_ReturnsGoodRecord()
    {
        var results = Parse("0A1B2C  good");
        Assert.Single(results);
        var (rec, err) = results[0];
        Assert.Null(err);
        Assert.Equal("A1B2C", rec.SerialNumber);
        Assert.Equal(CertificateStatus.Good, rec.Status);
    }

    [Fact]
    public void Parse_RevokedWithDate_SetsRevocationDate()
    {
        var results = Parse("FFEE  revoked  2024-06-15T00:00:00Z");
        var (rec, err) = results[0];
        Assert.Null(err);
        Assert.Equal(CertificateStatus.Revoked, rec.Status);
        Assert.NotNull(rec.RevocationDate);
        Assert.Equal(2024, rec.RevocationDate!.Value.Year);
    }

    [Fact]
    public void Parse_UnknownStatus_ReturnsUnknown()
    {
        var (rec, err) = Parse("0001  unknown")[0];
        Assert.Null(err);
        Assert.Equal(CertificateStatus.Unknown, rec.Status);
    }

    [Fact]
    public void Parse_AliasValid_TreatedAsGood()
    {
        var (rec, err) = Parse("0001  valid")[0];
        Assert.Null(err);
        Assert.Equal(CertificateStatus.Good, rec.Status);
    }

    [Fact]
    public void Parse_CommentLines_AreSkipped()
    {
        var results = Parse("# comment\n\n0001 good");
        Assert.Single(results);
    }

    [Fact]
    public void Parse_MissingStatus_ReturnsError()
    {
        var (_, err) = Parse("0001")[0];
        Assert.NotNull(err);
    }

    [Fact]
    public void Parse_LeadingZerosStripped()
    {
        var (rec, _) = Parse("000ABC good")[0];
        Assert.Equal("ABC", rec.SerialNumber);
    }

    [Fact]
    public void Parse_IssuerHashPopulated()
    {
        var (rec, _) = Parse("0001 good")[0];
        Assert.Equal("AABBCCDD", rec.IssuerKeyHashSha1);
    }
}
