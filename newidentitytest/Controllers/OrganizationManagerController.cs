using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for organisasjonsledere (OrganizationManager).
    /// Gir dashboard med statistikk over rapporter fra organisasjonens medlemmer.
    /// Krever OrganizationManager-rolle. Hver organisasjonsleder kan kun se data fra sin egen organisasjon.
    /// </summary>
    [Authorize(Roles = "OrganizationManager")]
    public class OrganizationManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

        /// <summary>
        /// Initialiserer controlleren med ApplicationDbContext for databaseoperasjoner.
        /// </summary>
        public OrganizationManagerController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Viser dashboard for organisasjonslederen med statistikk over rapporter fra organisasjonens medlemmer.
        /// Henter organisasjonen som den innloggede brukeren tilhører og beregner:
        /// - Totalt antall rapporter
        /// - Antall ventende rapporter (Pending)
        /// - Antall godkjente rapporter (Approved)
        /// - Antall avslåtte rapporter (Rejected)
        /// Returnerer Forbid hvis brukeren ikke har gyldig userId.
        /// Returnerer NotFound hvis brukeren ikke tilhører en organisasjon eller organisasjonen ikke finnes.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var user = await _db.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.OrganizationId == null)
            {
                return NotFound("User organization not found");
            }

            var organization = user.Organization;
            if (organization == null)
            {
                return NotFound("Organization not found");
            }

            // Hent alle bruker-IDer som tilhører denne organisasjonen
            var organizationUserIds = await _db.Users
                .Where(u => u.OrganizationId == organization.Id)
                .Select(u => u.Id)
                .ToListAsync();

            // Beregn statistikk over rapporter fra organisasjonens medlemmer
            var totalReports = await _db.Reports
                .Where(r => organizationUserIds.Contains(r.UserId))
                .CountAsync();

            var pendingReports = await _db.Reports
                .Where(r => organizationUserIds.Contains(r.UserId) && r.Status == "Pending")
                .CountAsync();

            var approvedReports = await _db.Reports
                .Where(r => organizationUserIds.Contains(r.UserId) && r.Status == "Approved")
                .CountAsync();

            var rejectedReports = await _db.Reports
                .Where(r => organizationUserIds.Contains(r.UserId) && r.Status == "Rejected")
                .CountAsync();

            ViewBag.Organization = organization;
            ViewBag.TotalReports = totalReports;
            ViewBag.PendingReports = pendingReports;
            ViewBag.ApprovedReports = approvedReports;
            ViewBag.RejectedReports = rejectedReports;

            return View();
        }
    }
}

