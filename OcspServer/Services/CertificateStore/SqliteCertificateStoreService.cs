using Microsoft.Data.Sqlite;
using OcspServer.Models;

namespace OcspServer.Services.CertificateStore
{
    /// <summary>
    /// SQLite-backed certificate store. The database file is created on first use.
    /// All methods are safe to call from multiple concurrent requests; SQLite
    /// serialises writers automatically when WAL mode is enabled.
    /// </summary>
    public class SqliteCertificateStoreService : ICertificateStoreService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteCertificateStoreService> _logger;

        public SqliteCertificateStoreService(string dbPath, ILogger<SqliteCertificateStoreService> logger)
        {
            _connectionString = $"Data Source={dbPath};";
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // Enable WAL for better concurrent read performance
            await ExecuteNonQueryAsync(conn, "PRAGMA journal_mode=WAL;");

            await ExecuteNonQueryAsync(conn, @"
                CREATE TABLE IF NOT EXISTS certificates (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    serial_number       TEXT    NOT NULL,
                    issuer_key_sha1     TEXT    NOT NULL,
                    issuer_key_sha256   TEXT    NOT NULL DEFAULT '',
                    issuer_name_sha1    TEXT    NOT NULL DEFAULT '',
                    issuer_dn           TEXT    NOT NULL DEFAULT '',
                    subject_dn          TEXT    NOT NULL DEFAULT '',
                    status              INTEGER NOT NULL DEFAULT 0,
                    revocation_date     TEXT,
                    revocation_reason   INTEGER NOT NULL DEFAULT 0,
                    not_before          TEXT    NOT NULL DEFAULT '',
                    not_after           TEXT    NOT NULL DEFAULT '',
                    import_source       TEXT    NOT NULL DEFAULT '',
                    last_updated        TEXT    NOT NULL,
                    notes               TEXT,
                    UNIQUE(serial_number, issuer_key_sha1)
                );
                CREATE INDEX IF NOT EXISTS idx_cert_serial_issuer
                    ON certificates(serial_number, issuer_key_sha1);
                CREATE INDEX IF NOT EXISTS idx_cert_status ON certificates(status);
            ");

            await ExecuteNonQueryAsync(conn, @"
                CREATE TABLE IF NOT EXISTS meta (
                    key   TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );
            ");

            _logger.LogInformation("SQLite certificate store initialised at {CS}", _connectionString);
        }

        public async Task<CertificateRecord?> FindBySerialAndIssuerKeyHashAsync(
            string serialHex, string issuerKeyHashHex)
        {
            var normalized = serialHex.ToUpperInvariant().TrimStart('0');
            var hashUpper = issuerKeyHashHex.ToUpperInvariant();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM certificates
                WHERE serial_number = @serial
                  AND (issuer_key_sha1 = @hash OR issuer_key_sha256 = @hash)
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@serial", normalized);
            cmd.Parameters.AddWithValue("@hash", hashUpper);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapRecord(reader);

            return null;
        }

        public async Task<IReadOnlyList<CertificateRecord>> GetAllAsync(
            int skip = 0, int take = 100, string? searchTerm = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var like = $"%{searchTerm}%";
                cmd.CommandText = @"
                    SELECT * FROM certificates
                    WHERE serial_number LIKE @term OR subject_dn LIKE @term
                    ORDER BY last_updated DESC
                    LIMIT @take OFFSET @skip;";
                cmd.Parameters.AddWithValue("@term", like);
            }
            else
            {
                cmd.CommandText = @"
                    SELECT * FROM certificates
                    ORDER BY last_updated DESC
                    LIMIT @take OFFSET @skip;";
            }
            cmd.Parameters.AddWithValue("@take", take);
            cmd.Parameters.AddWithValue("@skip", skip);

            var results = new List<CertificateRecord>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(MapRecord(reader));
            return results;
        }

        public async Task<int> CountAsync(string? searchTerm = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var like = $"%{searchTerm}%";
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM certificates
                    WHERE serial_number LIKE @term OR subject_dn LIKE @term;";
                cmd.Parameters.AddWithValue("@term", like);
            }
            else
            {
                cmd.CommandText = "SELECT COUNT(*) FROM certificates;";
            }
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<CertificateRecord?> GetByIdAsync(long id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM certificates WHERE id = @id LIMIT 1;";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapRecord(reader);
            return null;
        }

        public async Task UpsertAsync(CertificateRecord record)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            await UpsertCoreAsync(conn, record);
        }

        public async Task BulkUpsertAsync(IEnumerable<CertificateRecord> records)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var record in records)
                    await UpsertCoreAsync(conn, record, tx);

                tx.Commit();
                await SetMetaAsync(conn, "last_import", DateTime.UtcNow.ToString("O"));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task UpdateStatusAsync(long id, CertificateStatus status,
            DateTime? revocationDate, RevocationReason reason, string source)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE certificates
                SET status = @status,
                    revocation_date = @revDate,
                    revocation_reason = @reason,
                    import_source = @source,
                    last_updated = @updated
                WHERE id = @id;";
            cmd.Parameters.AddWithValue("@status", (int)status);
            cmd.Parameters.AddWithValue("@revDate",
                revocationDate.HasValue ? revocationDate.Value.ToString("O") : DBNull.Value);
            cmd.Parameters.AddWithValue("@reason", (int)reason);
            cmd.Parameters.AddWithValue("@source", source);
            cmd.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateNotesAsync(long id, string? notes)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE certificates SET notes = @notes WHERE id = @id;";
            cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DateTime?> GetLastImportTimeAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM meta WHERE key = 'last_import' LIMIT 1;";
            var val = await cmd.ExecuteScalarAsync() as string;
            if (val != null && DateTime.TryParse(val, out var dt))
                return dt;
            return null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static async Task UpsertCoreAsync(
            SqliteConnection conn, CertificateRecord r,
            SqliteTransaction? tx = null)
        {
            using var cmd = conn.CreateCommand();
            if (tx != null) cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO certificates
                    (serial_number, issuer_key_sha1, issuer_key_sha256, issuer_name_sha1,
                     issuer_dn, subject_dn, status, revocation_date, revocation_reason,
                     not_before, not_after, import_source, last_updated, notes)
                VALUES
                    (@serial, @sha1, @sha256, @nameSha1,
                     @issuer, @subject, @status, @revDate, @reason,
                     @notBefore, @notAfter, @source, @updated, @notes)
                ON CONFLICT(serial_number, issuer_key_sha1) DO UPDATE SET
                    issuer_key_sha256 = excluded.issuer_key_sha256,
                    issuer_name_sha1  = excluded.issuer_name_sha1,
                    issuer_dn         = excluded.issuer_dn,
                    subject_dn        = excluded.subject_dn,
                    status            = excluded.status,
                    revocation_date   = excluded.revocation_date,
                    revocation_reason = excluded.revocation_reason,
                    not_before        = excluded.not_before,
                    not_after         = excluded.not_after,
                    import_source     = excluded.import_source,
                    last_updated      = excluded.last_updated,
                    notes             = COALESCE(certificates.notes, excluded.notes);";

            cmd.Parameters.AddWithValue("@serial",
                r.SerialNumber.ToUpperInvariant().TrimStart('0'));
            cmd.Parameters.AddWithValue("@sha1", r.IssuerKeyHashSha1.ToUpperInvariant());
            cmd.Parameters.AddWithValue("@sha256", r.IssuerKeyHashSha256.ToUpperInvariant());
            cmd.Parameters.AddWithValue("@nameSha1", r.IssuerNameHashSha1.ToUpperInvariant());
            cmd.Parameters.AddWithValue("@issuer", r.IssuerDN);
            cmd.Parameters.AddWithValue("@subject", r.SubjectDN);
            cmd.Parameters.AddWithValue("@status", (int)r.Status);
            cmd.Parameters.AddWithValue("@revDate",
                r.RevocationDate.HasValue ? r.RevocationDate.Value.ToString("O") : DBNull.Value);
            cmd.Parameters.AddWithValue("@reason", (int)r.RevocationReason);
            cmd.Parameters.AddWithValue("@notBefore", r.NotBefore.ToString("O"));
            cmd.Parameters.AddWithValue("@notAfter", r.NotAfter.ToString("O"));
            cmd.Parameters.AddWithValue("@source", r.ImportSource);
            cmd.Parameters.AddWithValue("@updated", r.LastUpdated.ToString("O"));
            cmd.Parameters.AddWithValue("@notes", (object?)r.Notes ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task SetMetaAsync(SqliteConnection conn, string key, string value)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO meta(key, value) VALUES(@key, @value)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task ExecuteNonQueryAsync(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        private static CertificateRecord MapRecord(SqliteDataReader r)
        {
            var rec = new CertificateRecord
            {
                Id = r.GetInt64(r.GetOrdinal("id")),
                SerialNumber = r.GetString(r.GetOrdinal("serial_number")),
                IssuerKeyHashSha1 = r.GetString(r.GetOrdinal("issuer_key_sha1")),
                IssuerKeyHashSha256 = r.GetString(r.GetOrdinal("issuer_key_sha256")),
                IssuerNameHashSha1 = r.GetString(r.GetOrdinal("issuer_name_sha1")),
                IssuerDN = r.GetString(r.GetOrdinal("issuer_dn")),
                SubjectDN = r.GetString(r.GetOrdinal("subject_dn")),
                Status = (CertificateStatus)r.GetInt32(r.GetOrdinal("status")),
                RevocationReason = (RevocationReason)r.GetInt32(r.GetOrdinal("revocation_reason")),
                ImportSource = r.GetString(r.GetOrdinal("import_source")),
                LastUpdated = DateTime.Parse(r.GetString(r.GetOrdinal("last_updated"))),
            };

            var revDate = r.GetOrdinal("revocation_date");
            if (!r.IsDBNull(revDate))
                rec.RevocationDate = DateTime.Parse(r.GetString(revDate));

            var notBefore = r.GetString(r.GetOrdinal("not_before"));
            if (!string.IsNullOrEmpty(notBefore))
                rec.NotBefore = DateTime.Parse(notBefore);

            var notAfter = r.GetString(r.GetOrdinal("not_after"));
            if (!string.IsNullOrEmpty(notAfter))
                rec.NotAfter = DateTime.Parse(notAfter);

            var notesOrd = r.GetOrdinal("notes");
            if (!r.IsDBNull(notesOrd))
                rec.Notes = r.GetString(notesOrd);

            return rec;
        }
    }
}
