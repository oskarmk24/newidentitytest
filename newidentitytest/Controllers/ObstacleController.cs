using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;
using Microsoft.AspNetCore.Authorization; // Autorisasjon for å kreve innlogging
using System.Security.Claims; // Gir tilgang til brukerens Claims (f.eks. NameIdentifier)

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller som håndterer skjema for å registrere hinder.
    /// </summary>
    [Authorize] // Krev at brukeren er innlogget for å bruke disse endepunktene
    public class ObstacleController : Controller
    {
        // Repository brukes til å lagre data i databasen
        private readonly ApplicationDbContext _dbContext;

        public ObstacleController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Viser skjemaet for å registrere et nytt hinder (GET).
        /// </summary>
        [HttpGet]
        public ActionResult DataForm(int? id = null)
        {
            if (id.HasValue)
            {
                ViewBag.DraftId = id.Value;
            }
            return View();
        }

        /// <summary>
        /// Tar imot skjema-data (POST).
        /// Hvis data er gyldige lagres de i databasen og vi viser en oversiktsside.
        /// Hvis noe er feil, vises skjemaet på nytt med feilmeldinger.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(ObstacleData obstacleData, string action = "submit", int? id = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            bool isDraft = action == "draft";
            
            // For drafts: fjern valideringsfeil for alle felt unntatt ObstacleLocation
            if (isDraft)
            {
                ModelState.Remove(nameof(ObstacleData.ObstacleType));
                ModelState.Remove(nameof(ObstacleData.ObstacleHeight));
                ModelState.Remove(nameof(ObstacleData.ObstacleDescription));
                
                // Valider kun lokasjon for drafts
                if (string.IsNullOrWhiteSpace(obstacleData.ObstacleLocation))
                {
                    ModelState.AddModelError(nameof(ObstacleData.ObstacleLocation), "Map position is required.");
                    if (id.HasValue) ViewBag.DraftId = id.Value;
                    return View(obstacleData);
                }
            }
            else
            {
                // For full innsending: valider alle felt
                if (!ModelState.IsValid)
                {
                    if (id.HasValue) ViewBag.DraftId = id.Value;
                    return View(obstacleData);
                }
            }

            try
            {
                Report report;
                
                // Hvis id er satt, oppdater eksisterende draft
                if (id.HasValue)
                {
                    report = await _dbContext.Reports
                        .FirstOrDefaultAsync(r => r.Id == id.Value && r.UserId == userId && r.Status == "Draft");
                    
                    if (report == null) return NotFound();
                    
                    // Oppdater eksisterende draft - kun oppdater felt som har verdier
                    report.ObstacleLocation = obstacleData.ObstacleLocation;
                    
                    if (!string.IsNullOrWhiteSpace(obstacleData.ObstacleType))
                        report.ObstacleType = obstacleData.ObstacleType;
                    
                    if (obstacleData.ObstacleHeight > 0)
                        report.ObstacleHeight = Convert.ToInt32(Math.Round(obstacleData.ObstacleHeight));
                    
                    if (!string.IsNullOrWhiteSpace(obstacleData.ObstacleDescription))
                        report.ObstacleDescription = obstacleData.ObstacleDescription;
                }
                else
                {
                    // Opprett ny rapport/draft
                    report = new Report
                    {
                        ObstacleLocation = obstacleData.ObstacleLocation,
                        ObstacleType = string.IsNullOrWhiteSpace(obstacleData.ObstacleType) ? null : obstacleData.ObstacleType,
                        ObstacleHeight = obstacleData.ObstacleHeight > 0 ? Convert.ToInt32(Math.Round(obstacleData.ObstacleHeight)) : null,
                        ObstacleDescription = string.IsNullOrWhiteSpace(obstacleData.ObstacleDescription) ? null : obstacleData.ObstacleDescription,
                        UserId = userId
                    };
                    _dbContext.Reports.Add(report);
                }
                
                report.Status = isDraft ? "Draft" : "Pending";
                await _dbContext.SaveChangesAsync();
                
                if (isDraft)
                {
                    TempData["Success"] = "Draft saved successfully.";
                    return RedirectToAction(nameof(Drafts));
                }
                
                ViewBag.SavedReportId = report.Id;
                TempData["Success"] = "Report saved with ID " + report.Id;
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Database error: " + ex.Message);
                if (id.HasValue) ViewBag.DraftId = id.Value;
                return View(obstacleData);
            }

            return View("Overview", obstacleData);
        }

        [HttpGet]
        [Authorize(Roles = "Pilot")]
        public async Task<IActionResult> Drafts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var drafts = await _dbContext.Reports
                .Where(r => r.UserId == userId && r.Status == "Draft")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(drafts);
        }

        [HttpGet]
        [Authorize(Roles = "Pilot")]
        public async Task<IActionResult> EditDraft(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var draft = await _dbContext.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.Status == "Draft");
            
            if (draft == null)
            {
                return NotFound();
            }

            // Konverter Report til ObstacleData
            var obstacleData = new ObstacleData
            {
                ObstacleType = draft.ObstacleType ?? "",
                ObstacleHeight = draft.ObstacleHeight ?? 0,
                ObstacleDescription = draft.ObstacleDescription ?? "",
                ObstacleLocation = draft.ObstacleLocation ?? ""
            };

            ViewBag.DraftId = id;
            return View("DataForm", obstacleData);
        }

        [HttpGet]
        [Route("/api/obstacles")]
        public async Task<IActionResult> GetObstaclesForMap()
        {
            var reports = await _dbContext.Reports
                .Where(r => !string.IsNullOrEmpty(r.ObstacleLocation))
                .Select(r => new
                {
                    Id = r.Id,
                    Type = r.ObstacleType ?? "Unknown",
                    Height = r.ObstacleHeight,
                    Location = r.ObstacleLocation
                })
                .ToListAsync();
            
            return Ok(reports);
        }
        [HttpGet]
        [Route("/api/obstacles/approved")]
        [AllowAnonymous]
        public async Task<IActionResult> GetApprovedObstacles()
        {
            var approvedReports = await _dbContext.Reports
                .Where(r => r.Status == "Approved" && !string.IsNullOrEmpty(r.ObstacleLocation))
                .Select(r => new
                {
                    Id = r.Id,
                    Type = r.ObstacleType ?? "Unknown",
                    Height = r.ObstacleHeight,
                    Location = r.ObstacleLocation
                })
                .ToListAsync();
            
            return Ok(approvedReports);
        }
    }
}
