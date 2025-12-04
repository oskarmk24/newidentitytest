using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for visning og behandling av rapporter.
    /// Støtter visning av alle rapporter med sortering og søk, detaljvisning, godkjenning/avslag,
    /// tildeling til registerførere og sletting av rapporter.
    /// Krever autentisering for visning. Spesifikke operasjoner (Approve, Reject, Delete, AssignRegistrar)
    /// krever Registrar eller Admin rolle.
    /// </summary>
    [Authorize]
    public class ReportsController : Controller
    {
        // EF Core database context injected via DI container
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initialiserer controlleren med ApplicationDbContext og UserManager for databaseoperasjoner og brukerhåndtering.
        /// </summary>
        public ReportsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            // Store the injected DbContext for later queries
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// Viser liste over alle rapporter med sortering og søkefunksjonalitet.
        /// Inkluderer informasjon om avsender og organisasjon via joins.
        /// Støtter sortering etter: id, CreatedAt, Sender, ObstacleType, Status (standard: CreatedAt desc).
        /// Støtter søk i: Id, Sender, OrganizationName, ObstacleType, CreatedAt (dato), Status.
        /// Søkefilteret anvendes i minnet for å unngå EF Core oversettelsesproblemer med komplekse strengoperasjoner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
        {
            // Build query with joins to get user and organization info
            var query = from r in _db.Reports
                        join u in _db.Users on r.UserId equals u.Id into userJoin
                        from u in userJoin.DefaultIfEmpty()
                        join o in _db.Organizations on u.OrganizationId equals o.Id into orgJoin
                        from o in orgJoin.DefaultIfEmpty()
                        select new ReportListItem
                        {
                            Id = r.Id,
                            CreatedAt = r.CreatedAt,
                            Sender = u != null ? (u.Email ?? u.UserName) : "(unknown)",
                            OrganizationName = o != null ? o.Name : null,
                            ObstacleType = r.ObstacleType,
                            Status = r.Status,
                            ObstacleLocation = r.ObstacleLocation
                        };

            // På samme måte som i OrganizationController og PilotController legger vi til sortering og søkefunksjonalitet
            query = sortBy.ToLower() switch
            {
                "id" => sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id),
                "createdat" => sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
                "sender" => sortOrder == "asc" ? query.OrderBy(r => r.Sender) : query.OrderByDescending(r => r.Sender),
                "organizationname" => sortOrder == "asc" ? query.OrderBy(r => r.OrganizationName ?? "") : query.OrderByDescending(r => r.OrganizationName ?? ""),
                "obstacletype" => sortOrder == "asc" ? query.OrderBy(r => r.ObstacleType ?? "") : query.OrderByDescending(r => r.ObstacleType ?? ""),
                "status" => sortOrder == "asc" ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var items = await query.ToListAsync();

            
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                items = items.Where(r =>
                    r.Id.ToString().Contains(searchLower) ||
                    (r.Sender ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                    (r.OrganizationName ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                    ((r.ObstacleType ?? string.Empty).ToLowerInvariant().Contains(searchLower)) ||
                    r.CreatedAt.ToString("MMM dd, yyyy").ToLowerInvariant().Contains(searchLower) ||
                    ((r.Status ?? string.Empty).ToLowerInvariant().Contains(searchLower))
            ).ToList();

                
            }
            
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Search = search;
            
            return View(items);
        }

        /// <summary>
        /// API-endepunkt som returnerer alle rapporter som JSON.
        /// Brukes av verktøy og diagnostikk. Returnerer rapporter sortert etter opprettelsesdato (nyeste først).
        /// Krever autentisering (arver fra [Authorize] på controller-nivå).
        /// </summary>
        [HttpGet("/api/reports")]
        public async Task<IActionResult> GetAll()
        {
            // Basic JSON endpoint used by tooling/diagnostics
            var reports = await _db.Reports
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(reports);
        }


        /// <summary>
        /// Viser detaljvisning av en spesifikk rapport.
        /// Inkluderer informasjon om avsender og tildelt registerfører.
        /// Hvis brukeren har Registrar-rolle, lastes liste over tilgjengelige registerførere for tildeling.
        /// Returnerer NotFound hvis rapporten ikke finnes.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Load a single report by its primary key
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            // Try to resolve the sender (Identity user) for display
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == report.UserId);
            var assignedRegistrar = string.Empty;
            if (!string.IsNullOrEmpty(report.AssignedRegistrarId))
            {
                var registrarUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == report.AssignedRegistrarId);
                if (registrarUser != null)
                {
                    assignedRegistrar = registrarUser.Email ?? registrarUser.UserName ?? "(unknown)";
                }
            }

            if (User.IsInRole("Registrar"))
            {
                var registrarUsers = await _userManager.GetUsersInRoleAsync("Registrar");
                ViewBag.RegistrarOptions = registrarUsers
                    .Select(r => new
                    {
                        Id = r.Id,
                        Name = r.Email ?? r.UserName ?? "(unknown)"
                    })
                    .ToList();
            }

            ViewBag.AssignedRegistrarName = string.IsNullOrEmpty(assignedRegistrar) ? null : assignedRegistrar;

            var vm = new ReportDetailsViewModel
            {
                Report = report,
                Sender = user?.Email ?? user?.UserName ?? "(unknown)"
            };

            return View(vm);
        }
        /// <summary>
        /// Godkjenner en rapport og oppdaterer status til "Approved".
        /// Oppdaterer ProcessedAt til nåværende tid og nullstiller eventuell tidligere avslagsbegrunnelse.
        /// Oppretter notifikasjon til piloten om at rapporten er godkjent.
        /// Redirecter tilbake til Details med suksessmelding.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Registrar,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction(nameof(Index));
            }

            // Rpport status oppdateres
            report.Status = "Approved";
            report.ProcessedAt = DateTime.UtcNow;
            report.RejectionReason = null; // Clear any previous rejection reason

            await _db.SaveChangesAsync();

            // Lager notifikasjon for piloten
            await CreateNotificationForPilotAsync(report, "Approved", null);

            TempData["SuccessMessage"] = "Report approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Avslår en rapport med begrunnelse og oppdaterer status til "Rejected".
        /// Krever Registrar eller Admin rolle.
        /// Validerer at avslagsbegrunnelse er oppgitt.
        /// Oppdaterer ProcessedAt til nåværende tid og lagrer avslagsbegrunnelsen.
        /// Oppretter notifikasjon til piloten om at rapporten er avslått med begrunnelse.
        /// Redirecter tilbake til Details med suksessmelding eller feilmelding hvis begrunnelse mangler.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Registrar,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction(nameof(Index));
            }

            // Validering av avslagsbegrunnelse
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "You must provide a reason for rejection.";
                return RedirectToAction(nameof(Details), new { id });
            }

            report.Status = "Rejected";
            report.RejectionReason = rejectionReason;
            report.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await CreateNotificationForPilotAsync(report, "Rejected", rejectionReason);

            TempData["SuccessMessage"] = "Report rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Oppretter en notifikasjon til piloten når deres rapport er behandlet (godkjent eller avslått).
        /// Henter navnet på registerføreren/admin som behandlet rapporten.
        /// Oppretter notifikasjon med passende tittel og melding basert på status.
        /// Hvis rapporten er avslått, inkluderes avslagsbegrunnelsen i meldingen.
        /// Returnerer uten å gjøre noe hvis rapporten ikke har en tilknyttet userId.
        /// </summary>
        private async Task CreateNotificationForPilotAsync(Report report, string status, string? rejectionReason)
        {
            if (string.IsNullOrEmpty(report.UserId))
                return;

            // Get the name of the registrar/admin who processed the report
            var registrarName = "a registrar";
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var registrar = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (registrar != null)
                {
                    registrarName = registrar.Email ?? registrar.UserName ?? "a registrar";
                }
            }

            var title = status == "Approved" 
                ? $"Report #{report.Id} approved" 
                : $"Report #{report.Id} rejected";

            var message = status == "Approved"
                ? $"Your report #{report.Id} has been approved by {registrarName}."
                : $"Your report #{report.Id} has been rejected by {registrarName}. Reason: {rejectionReason}";

            var notification = new Notification
            {
                UserId = report.UserId,
                ReportId = report.Id,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Tildeler eller fjerner tildeling av en registerfører til en rapport.
        /// Hvis registrarId er null eller tom, fjernes tildelingen.
        /// Hvis registrarId er oppgitt, valideres at brukeren eksisterer og har Registrar-rolle.
        /// Redirecter tilbake til Details med suksessmelding eller feilmelding hvis validering feiler.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Registrar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRegistrar(int id, string? registrarId)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(registrarId))
            {
                report.AssignedRegistrarId = null;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Assignment removed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var registrarUser = await _userManager.FindByIdAsync(registrarId);
            if (registrarUser == null || !await _userManager.IsInRoleAsync(registrarUser, "Registrar"))
            {
                TempData["ErrorMessage"] = "Invalid registrar selected.";
                return RedirectToAction(nameof(Details), new { id });
            }

            report.AssignedRegistrarId = registrarUser.Id;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report assigned.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Sletter en rapport permanent fra databasen.
        /// Sletter først alle tilknyttede notifikasjoner, deretter selve rapporten.
        /// Oppretter en notifikasjon til piloten om at rapporten er slettet (hvis piloten eksisterer).
        /// Redirecter til Index med suksessmelding eller feilmelding hvis rapporten ikke finnes.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Registrar,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction(nameof(Index));
            }

            // Store report info for notification before deletion
            var reportId = report.Id;
            var userId = report.UserId;

            // Slett tilknyttede notifikasjoner først
            var notifications = await _db.Notifications
                .Where(n => n.ReportId == reportId)
                .ToListAsync();
            _db.Notifications.RemoveRange(notifications);

            // slett rapport
            _db.Reports.Remove(report);
            await _db.SaveChangesAsync();

            // Lager notifikasjon for piloten
            if (!string.IsNullOrEmpty(userId))
            {
                var registrarName = "a registrar";
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var registrar = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (registrar != null)
                    {
                        registrarName = registrar.Email ?? registrar.UserName ?? "a registrar";
                    }
                }

                var notification = new Notification
                {
                    UserId = userId,
                    ReportId = reportId,
                    Title = $"Report #{reportId} deleted",
                    Message = $"Your report #{reportId} has been deleted by {registrarName}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Report #{reportId} has been deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

}
