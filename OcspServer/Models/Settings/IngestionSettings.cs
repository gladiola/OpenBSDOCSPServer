namespace OcspServer.Models.Settings
{
    /// <summary>
    /// Settings for automated ingestion of certificate status data.
    /// </summary>
    public class IngestionSettings
    {
        /// <summary>
        /// Path to the OpenSSL CA index.txt file to watch for changes.
        /// Leave empty to disable automatic file-watch ingestion.
        /// </summary>
        public string? IndexTxtWatchPath { get; set; }

        /// <summary>
        /// How often (in minutes) to poll index.txt for changes.
        /// 0 = manual import only.
        /// </summary>
        public int PollingIntervalMinutes { get; set; } = 0;

        /// <summary>
        /// Base URL of a running local OpenSSL OCSP responder for live delta sync.
        /// Example: "http://127.0.0.1:2560"
        /// </summary>
        public string? LocalOcspResponderUrl { get; set; }

        /// <summary>SQLite database file path. Defaults to "ocsp.db" in the app directory.</summary>
        public string DatabasePath { get; set; } = "ocsp.db";
    }
}
