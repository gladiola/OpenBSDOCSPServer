using OcspServer.Models;
using OcspServer.Models.Settings;
using OcspServer.Services.CertificateStore;

namespace OcspServer.Services.Ingestion
{
    /// <summary>
    /// Background service that watches the OpenSSL index.txt file for changes and
    /// automatically re-imports it into the certificate store.
    ///
    /// Enabled only when <see cref="FeatureFlags.EnableIndexTxtWatch"/> is true and
    /// <see cref="IngestionSettings.IndexTxtWatchPath"/> is configured.
    ///
    /// The service uses <see cref="FileSystemWatcher"/> for immediate change detection,
    /// with an optional polling fallback via <see cref="IngestionSettings.PollingIntervalMinutes"/>.
    /// A brief debounce delay is applied to coalesce rapid successive writes.
    /// </summary>
    public class IndexTxtWatcherService : BackgroundService
    {
        private readonly IngestionSettings _ingestionSettings;
        private readonly ICertificateStoreService _store;
        private readonly ILogger<IndexTxtWatcherService> _logger;

        // Issuer metadata is read from appsettings at startup.
        private readonly string _issuerDn;
        private readonly string _issuerKeyHashSha1;
        private readonly string _issuerKeyHashSha256;

        // Semaphore to prevent concurrent imports when both watcher and poll fire.
        private readonly SemaphoreSlim _importLock = new(1, 1);

        // Debounce: wait this long after the last write event before importing.
        private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(3);
        private CancellationTokenSource? _debounceCts;

        public IndexTxtWatcherService(
            IngestionSettings ingestionSettings,
            ICertificateStoreService store,
            ILogger<IndexTxtWatcherService> logger,
            string issuerDn = "",
            string issuerKeyHashSha1 = "",
            string issuerKeyHashSha256 = "")
        {
            _ingestionSettings = ingestionSettings;
            _store = store;
            _logger = logger;
            _issuerDn = issuerDn;
            _issuerKeyHashSha1 = issuerKeyHashSha1;
            _issuerKeyHashSha256 = issuerKeyHashSha256;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var watchPath = _ingestionSettings.IndexTxtWatchPath;
            if (string.IsNullOrWhiteSpace(watchPath))
            {
                _logger.LogWarning(
                    "IndexTxtWatcherService: IndexTxtWatchPath is not configured – watcher will not start.");
                return;
            }

            if (!File.Exists(watchPath))
            {
                _logger.LogWarning(
                    "IndexTxtWatcherService: File not found at {Path} – watcher will wait for it to appear.",
                    watchPath);
            }

            _logger.LogInformation(
                "IndexTxtWatcherService: Watching {Path} for changes.", watchPath);

            // Do an initial import when the service starts.
            await ImportAsync(watchPath, stoppingToken);

            var directory = Path.GetDirectoryName(Path.GetFullPath(watchPath))!;
            var filename = Path.GetFileName(watchPath);

            using var watcher = new FileSystemWatcher(directory, filename)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Changed += (_, _) => OnFileChanged(watchPath, stoppingToken);
            watcher.Created += (_, _) => OnFileChanged(watchPath, stoppingToken);

            // Optional polling loop (fallback / belt-and-suspenders).
            int pollMinutes = _ingestionSettings.PollingIntervalMinutes;
            if (pollMinutes > 0)
            {
                _logger.LogInformation(
                    "IndexTxtWatcherService: Polling every {Interval} minutes.", pollMinutes);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (pollMinutes > 0)
                {
                    await Task.Delay(TimeSpan.FromMinutes(pollMinutes), stoppingToken)
                              .ConfigureAwait(false);
                    await ImportAsync(watchPath, stoppingToken);
                }
                else
                {
                    // No polling – just keep alive until cancellation.
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken)
                              .ConfigureAwait(false);
                }
            }
        }

        // ── Event debounce ────────────────────────────────────────────────────

        private void OnFileChanged(string filePath, CancellationToken stoppingToken)
        {
            // Cancel any pending debounce timer and restart it.
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var localCts = _debounceCts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelay, localCts.Token);
                    await ImportAsync(filePath, localCts.Token);
                }
                catch (OperationCanceledException) { /* cancelled by next change event */ }
            }, stoppingToken);
        }

        // ── Import logic ──────────────────────────────────────────────────────

        private async Task ImportAsync(string filePath, CancellationToken ct)
        {
            if (!await _importLock.WaitAsync(0, ct))
            {
                _logger.LogDebug("IndexTxtWatcherService: Import already in progress – skipping.");
                return;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("IndexTxtWatcherService: {Path} not found – skipping import.", filePath);
                    return;
                }

                _logger.LogInformation("IndexTxtWatcherService: Importing {Path} …", filePath);

                var parserLogger = Microsoft.Extensions.Logging.Abstractions
                    .NullLogger<OpenSslIndexParser>.Instance;

                var parser = new OpenSslIndexParser(
                    parserLogger,
                    _issuerDn,
                    _issuerKeyHashSha1,
                    _issuerKeyHashSha256);

                var records = new List<CertificateRecord>();
                var errors = new List<string>();

                await using (var fs = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    foreach (var (record, error) in parser.Parse(fs))
                    {
                        if (error != null)
                            errors.Add(error);
                        else
                            records.Add(record);
                    }
                }

                if (records.Count > 0)
                    await _store.BulkUpsertAsync(records);

                _logger.LogInformation(
                    "IndexTxtWatcherService: Import complete – {Added} records, {Errors} parse errors.",
                    records.Count, errors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IndexTxtWatcherService: Import failed for {Path}.", filePath);
            }
            finally
            {
                _importLock.Release();
            }
        }

        public override void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _importLock.Dispose();
            base.Dispose();
        }
    }
}
