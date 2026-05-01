using OcspServer.Models;

namespace OcspServer.Services.CertificateStore
{
    public interface ICertificateStoreService
    {
        /// <summary>Ensure the schema exists and apply any pending migrations.</summary>
        Task InitializeAsync();

        /// <summary>
        /// Look up a certificate by serial number and issuer key hash (SHA-1 or SHA-256).
        /// Returns null if not found.
        /// </summary>
        Task<CertificateRecord?> FindBySerialAndIssuerKeyHashAsync(string serialHex, string issuerKeyHashHex);

        /// <summary>Return all known certificate records (for admin UI).</summary>
        Task<IReadOnlyList<CertificateRecord>> GetAllAsync(int skip = 0, int take = 100, string? searchTerm = null);

        /// <summary>Total count (for pagination), optionally filtered.</summary>
        Task<int> CountAsync(string? searchTerm = null);

        /// <summary>Get a single record by its surrogate ID.</summary>
        Task<CertificateRecord?> GetByIdAsync(long id);

        /// <summary>Insert or update a record keyed on (SerialNumber, IssuerKeyHashSha1).</summary>
        Task UpsertAsync(CertificateRecord record);

        /// <summary>Bulk upsert – used during import operations.</summary>
        Task BulkUpsertAsync(IEnumerable<CertificateRecord> records);

        /// <summary>Update only the status fields of an existing record.</summary>
        Task UpdateStatusAsync(long id, CertificateStatus status, DateTime? revocationDate,
            RevocationReason reason, string source);

        /// <summary>Update admin notes for a record.</summary>
        Task UpdateNotesAsync(long id, string? notes);

        /// <summary>Timestamp of the most recent import.</summary>
        Task<DateTime?> GetLastImportTimeAsync();
    }
}
