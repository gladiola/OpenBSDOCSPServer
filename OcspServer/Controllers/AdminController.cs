using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OcspServer.Models;
using OcspServer.Models.Settings;
using OcspServer.Resources;
using OcspServer.Services.CertificateStore;
using OcspServer.Services.Ingestion;
using OcspServer.Services.Ocsp;

namespace OcspServer.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ICertificateStoreService _store;
        private readonly ILogger<AdminController> _logger;
        private readonly OcspServerSettings _ocspSettings;
        private readonly OcspSigningService _signingService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AdminController(
            ICertificateStoreService store,
            OcspServerSettings ocspSettings,
            OcspSigningService signingService,
            IStringLocalizer<SharedResource> localizer,
            ILogger<AdminController> logger)
        {
            _store = store;
            _ocspSettings = ocspSettings;
            _signingService = signingService;
            _localizer = localizer;
            _logger = logger;
        }

        // ── Dashboard ──────────────────────────────────────────────────────

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var all = await _store.GetAllAsync(0, int.MaxValue);
            var lastImport = await _store.GetLastImportTimeAsync();

            // Try to get signing cert expiry
            DateTime? signerExpiry = null;
            try
            {
                var (_, _, pubKey, chain) = _signingService.GetSigningMaterial();
                if (chain.Length > 0)
                    signerExpiry = chain[0].NotAfter.ToUniversalTime();
            }
            catch { /* credentials not yet configured */ }

            var vm = new DashboardViewModel
            {
                TotalCertificates = all.Count,
                GoodCount = all.Count(c => c.Status == CertificateStatus.Good),
                RevokedCount = all.Count(c => c.Status == CertificateStatus.Revoked),
                UnknownCount = all.Count(c => c.Status == CertificateStatus.Unknown),
                LastImportTime = lastImport,
                SignerCertExpiry = signerExpiry
            };
            return View(vm);
        }

        // ── Certificate list ────────────────────────────────────────────────

        [HttpGet("certificates")]
        public async Task<IActionResult> Certificates(string? search, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            int skip = (page - 1) * pageSize;
            var records = await _store.GetAllAsync(skip, pageSize, search);
            var total = await _store.CountAsync(search);

            var vm = new CertificateListViewModel
            {
                Records = records,
                Search = search,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
            return View(vm);
        }

        // ── Certificate detail ──────────────────────────────────────────────

        [HttpGet("certificates/{id:long}")]
        public async Task<IActionResult> Detail(long id)
        {
            var record = await _store.GetByIdAsync(id);
            if (record == null) return NotFound();
            return View(record);
        }

        [HttpPost("certificates/{id:long}/notes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotes(long id, string? notes)
        {
            var record = await _store.GetByIdAsync(id);
            if (record == null) return NotFound();
            await _store.UpdateNotesAsync(id, notes);
            TempData["Success"] = _localizer["SuccessNotesUpdated"];
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ── Revoke ──────────────────────────────────────────────────────────

        [HttpGet("certificates/{id:long}/revoke")]
        public async Task<IActionResult> Revoke(long id)
        {
            var record = await _store.GetByIdAsync(id);
            if (record == null) return NotFound();
            if (record.Status == CertificateStatus.Revoked)
            {
                TempData["Warning"] = _localizer["WarningAlreadyRevoked"];
                return RedirectToAction(nameof(Detail), new { id });
            }
            return View(record);
        }

        [HttpPost("certificates/{id:long}/revoke")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeConfirm(long id, RevocationReason reason)
        {
            var record = await _store.GetByIdAsync(id);
            if (record == null) return NotFound();

            await _store.UpdateStatusAsync(
                id,
                CertificateStatus.Revoked,
                DateTime.UtcNow,
                reason,
                "manual");

            _logger.LogWarning("Certificate REVOKED: serial={Serial} reason={Reason} by admin",
                record.SerialNumber, reason);

            TempData["Success"] = _localizer["SuccessCertificateRevoked", record.SerialNumber, reason];
            return RedirectToAction(nameof(Certificates));
        }

        // ── Reinstate ────────────────────────────────────────────────────────

        [HttpPost("certificates/{id:long}/reinstate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reinstate(long id)
        {
            var record = await _store.GetByIdAsync(id);
            if (record == null) return NotFound();

            if (record.Status != CertificateStatus.Revoked ||
                record.RevocationReason != RevocationReason.CertificateHold)
            {
                TempData["Warning"] =
                    _localizer["WarningOnlyCertificateHoldReinstate"];
                return RedirectToAction(nameof(Detail), new { id });
            }

            await _store.UpdateStatusAsync(
                id,
                CertificateStatus.Good,
                null,
                RevocationReason.Unspecified,
                "manual");

            _logger.LogInformation("Certificate REINSTATED: serial={Serial}", record.SerialNumber);
            TempData["Success"] = _localizer["SuccessCertificateReinstated", record.SerialNumber];
            return RedirectToAction(nameof(Certificates));
        }

        // ── Import ────────────────────────────────────────────────────────────

        [HttpGet("import")]
        public IActionResult Import() => View(new ImportViewModel());

        [HttpPost("import/indexfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportIndexFile(
            IFormFile file,
            string? issuerDn,
            string? issuerKeyHashSha1,
            string? issuerKeyHashSha256,
            [FromServices] ILogger<OpenSslIndexParser> parserLogger)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", _localizer["ErrorSelectIndexFile"]);
                return View("Import", new ImportViewModel());
            }

            var parser = new OpenSslIndexParser(
                parserLogger,
                issuerDn ?? string.Empty,
                issuerKeyHashSha1 ?? string.Empty,
                issuerKeyHashSha256 ?? string.Empty);

            var importResult = new ImportResult { Source = "index.txt" };
            var records = new List<CertificateRecord>();

            using (var stream = file.OpenReadStream())
            {
                foreach (var (record, error) in parser.Parse(stream))
                {
                    if (error != null)
                        importResult.Errors.Add(error);
                    else
                        records.Add(record);
                }
            }

            if (records.Count > 0)
            {
                await _store.BulkUpsertAsync(records);
                importResult.Added = records.Count;
            }

            _logger.LogInformation(
                "index.txt import: {Added} records, {Errors} errors",
                importResult.Added, importResult.Errors.Count);

            return View("ImportResult", importResult);
        }

        [HttpPost("import/textfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTextFile(
            IFormFile file,
            string? issuerKeyHashSha1,
            [FromServices] ILogger<TextFileImporter> importerLogger)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", _localizer["ErrorSelectTextFile"]);
                return View("Import", new ImportViewModel());
            }

            var importer = new TextFileImporter(
                importerLogger,
                string.Empty,
                issuerKeyHashSha1 ?? string.Empty);

            var importResult = new ImportResult { Source = "textfile" };
            var records = new List<CertificateRecord>();

            using (var stream = file.OpenReadStream())
            {
                foreach (var (record, error) in importer.Parse(stream))
                {
                    if (error != null)
                        importResult.Errors.Add(error);
                    else
                        records.Add(record);
                }
            }

            if (records.Count > 0)
            {
                await _store.BulkUpsertAsync(records);
                importResult.Added = records.Count;
            }

            return View("ImportResult", importResult);
        }

        [HttpPost("import/ocspproxy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportOcspProxy(
            string responderUrl,
            string requestHex,
            string issuerKeyHashSha1,
            [FromServices] LiveOcspProxyImporter proxyImporter)
        {
            if (string.IsNullOrWhiteSpace(responderUrl) || string.IsNullOrWhiteSpace(requestHex))
            {
                ModelState.AddModelError("", _localizer["ErrorResponderUrlAndRequestHexRequired"]);
                return View("Import", new ImportViewModel());
            }

            byte[] requestDer;
            try
            {
                requestDer = Convert.FromHexString(requestHex.Replace(" ", "").Replace(":", ""));
            }
            catch
            {
                ModelState.AddModelError("", _localizer["ErrorInvalidHexDerRequest"]);
                return View("Import", new ImportViewModel());
            }

            var importResult = await proxyImporter.SyncAsync(responderUrl, requestDer, issuerKeyHashSha1);
            return View("ImportResult", importResult);
        }
    }

    // ── View Models ────────────────────────────────────────────────────────────

    public class DashboardViewModel
    {
        public int TotalCertificates { get; set; }
        public int GoodCount { get; set; }
        public int RevokedCount { get; set; }
        public int UnknownCount { get; set; }
        public DateTime? LastImportTime { get; set; }
        public DateTime? SignerCertExpiry { get; set; }
        public bool SignerExpiringSoon =>
            SignerCertExpiry.HasValue &&
            SignerCertExpiry.Value < DateTime.UtcNow.AddDays(30);
    }

    public class CertificateListViewModel
    {
        public IReadOnlyList<CertificateRecord> Records { get; set; } = Array.Empty<CertificateRecord>();
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class ImportViewModel
    {
        public string? StatusMessage { get; set; }
    }
}
