using Microsoft.Data.Sqlite;
using OcspServer.Models;
using OcspServer.Services.CertificateStore;

namespace OcspServer.Tests;

public class SqliteCertificateStoreTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly SqliteCertificateStoreService _store;

    public SqliteCertificateStoreTests()
    {
        // Use a temp in-memory SQLite file per test class instance
        _dbPath = $"Data Source=file:memdb-{Guid.NewGuid()}?mode=memory&cache=shared";
        _store = new SqliteCertificateStoreService(
            _dbPath,
            Microsoft.Extensions.Logging.Abstractions
                .NullLogger<SqliteCertificateStoreService>.Instance);
    }

    public Task InitializeAsync() => _store.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static CertificateRecord MakeRecord(
        string serial = "AABBCC",
        string keyHash = "1122334455667788990011223344556677889900",
        CertificateStatus status = CertificateStatus.Good) =>
        new CertificateRecord
        {
            SerialNumber = serial,
            IssuerKeyHashSha1 = keyHash,
            IssuerKeyHashSha256 = string.Empty,
            IssuerNameHashSha1 = string.Empty,
            IssuerDN = "CN=Test CA",
            SubjectDN = "CN=Test Cert",
            Status = status,
            NotBefore = DateTime.UtcNow.AddYears(-1),
            NotAfter = DateTime.UtcNow.AddYears(1),
            ImportSource = "test",
            LastUpdated = DateTime.UtcNow
        };

    // ── Upsert / Find ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertAndFind_GoodCert_ReturnsRecord()
    {
        var record = MakeRecord();
        await _store.UpsertAsync(record);

        var found = await _store.FindBySerialAndIssuerKeyHashAsync(
            "AABBCC", "1122334455667788990011223344556677889900");

        Assert.NotNull(found);
        Assert.Equal("AABBCC", found!.SerialNumber);
        Assert.Equal(CertificateStatus.Good, found.Status);
    }

    [Fact]
    public async Task Find_UnknownSerial_ReturnsNull()
    {
        var found = await _store.FindBySerialAndIssuerKeyHashAsync(
            "DEADBEEF", "1122334455667788990011223344556677889900");
        Assert.Null(found);
    }

    [Fact]
    public async Task Find_NormalisesLeadingZeros()
    {
        await _store.UpsertAsync(MakeRecord(serial: "AABBCC"));
        // Query with leading zeros should still match
        var found = await _store.FindBySerialAndIssuerKeyHashAsync(
            "00AABBCC", "1122334455667788990011223344556677889900");
        Assert.NotNull(found);
    }

    [Fact]
    public async Task Upsert_Idempotent_DoesNotDuplicate()
    {
        await _store.UpsertAsync(MakeRecord());
        await _store.UpsertAsync(MakeRecord());
        var count = await _store.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BulkUpsert_InsertsMany()
    {
        var records = Enumerable.Range(1, 10)
            .Select(i => MakeRecord($"SERIAL{i:D4}"))
            .ToList();
        await _store.BulkUpsertAsync(records);
        Assert.Equal(10, await _store.CountAsync());
    }

    // ── Status update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ChangesStatusToRevoked()
    {
        await _store.UpsertAsync(MakeRecord());
        var all = await _store.GetAllAsync();
        var id = all[0].Id;

        await _store.UpdateStatusAsync(
            id,
            CertificateStatus.Revoked,
            DateTime.UtcNow,
            RevocationReason.KeyCompromise,
            "manual");

        var updated = await _store.GetByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal(CertificateStatus.Revoked, updated!.Status);
        Assert.Equal(RevocationReason.KeyCompromise, updated.RevocationReason);
        Assert.NotNull(updated.RevocationDate);
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNotes_PersistsNotes()
    {
        await _store.UpsertAsync(MakeRecord());
        var id = (await _store.GetAllAsync())[0].Id;

        await _store.UpdateNotesAsync(id, "test note");
        var rec = await _store.GetByIdAsync(id);
        Assert.Equal("test note", rec?.Notes);
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_SearchBySerial_FiltersResults()
    {
        await _store.BulkUpsertAsync(new[]
        {
            MakeRecord("AAAA11"),
            MakeRecord("BBBB22")
        });

        var results = await _store.GetAllAsync(0, 100, "AAAA");
        Assert.Single(results);
        Assert.Equal("AAAA11", results[0].SerialNumber);
    }

    // ── Last import time ──────────────────────────────────────────────────────

    [Fact]
    public async Task BulkUpsert_SetsLastImportTime()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await _store.BulkUpsertAsync(new[] { MakeRecord() });
        var lastImport = await _store.GetLastImportTimeAsync();
        Assert.NotNull(lastImport);
        Assert.True(lastImport > before);
    }
}
