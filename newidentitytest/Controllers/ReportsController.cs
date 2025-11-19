using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    // Require a logged-in user to view reports (protects list + details)
    [Authorize]
    public class ReportsController : Controller
    {
        // EF Core database context injected via DI container
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db)
        {
            // Store the injected DbContext for later queries
            _db = db;
        }

        // GET /Reports
        [HttpGet]
        public async Task<IActionResult> Index(string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
        {
            // Build query
            var query = from r in _db.Reports
                        join u in _db.Users on r.UserId equals u.Id into gj
                        from u in gj.DefaultIfEmpty()
                        select new ReportListItem
                        {
                            Id = r.Id,
                            CreatedAt = r.CreatedAt,
                            Sender = u != null ? (u.Email ?? u.UserName) : "(unknown)",
                            ObstacleType = r.ObstacleType,
                            Status = r.Status,
                            ObstacleLocation = r.ObstacleLocation
                        };

            // NOTE: applying complex string operations (ToLower/ToString formatting) inside an EF Core
            // LINQ expression can fail to translate to SQL on some providers. To keep the UI responsive
            // and make the search button work reliably, we'll materialize the projected results first
            // and then apply an in-memory filter if a search term is provided.

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "id" => sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id),
                "createdat" => sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
                "sender" => sortOrder == "asc" ? query.OrderBy(r => r.Sender) : query.OrderByDescending(r => r.Sender),
                "obstacletype" => sortOrder == "asc" ? query.OrderBy(r => r.ObstacleType ?? "") : query.OrderByDescending(r => r.ObstacleType ?? ""),
                "status" => sortOrder == "asc" ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var items = await query.ToListAsync();

            // Apply search filter in-memory to avoid EF translation issues
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                items = items.Where(r =>
                    r.Id.ToString().Contains(searchLower) ||
                    (r.Sender ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                    ((r.ObstacleType ?? string.Empty).ToLowerInvariant().Contains(searchLower)) ||
                    r.CreatedAt.ToString("MMM dd, yyyy").ToLowerInvariant().Contains(searchLower) ||
                    ((r.Status ?? string.Empty).ToLowerInvariant().Contains(searchLower))
            ).ToList();

                
            }
            
            // Pass sorting info to view
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Search = search;
            
            return View(items);
        }

        // GET /api/reports
        [HttpGet("/api/reports")]
        public async Task<IActionResult> GetAll()
        {
            // Basic JSON endpoint used by tooling/diagnostics
            var reports = await _db.Reports
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(reports);
        }


        // GET /Reports/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Load a single report by its primary key
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            // Try to resolve the sender (Identity user) for display
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == report.UserId);
            var vm = new ReportDetailsViewModel
            {
                Report = report,
                Sender = user?.Email ?? user?.UserName ?? "(unknown)"
            };

            return View(vm);
        }
        /// <summary>
        /// POST: Approve a report
        /// Only accessible to users with Registrar or Admin role
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

            // Update report status
            report.Status = "Approved";
            report.ProcessedAt = DateTime.UtcNow;
            report.RejectionReason = null; // Clear any previous rejection reason

            await _db.SaveChangesAsync();

            // Create notification for pilot
            await CreateNotificationForPilotAsync(report, "Approved", null);

            TempData["SuccessMessage"] = "Report approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// POST: Reject a report with a reason
        /// Only accessible to users with Registrar or Admin role
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

            // Validate rejection reason
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "You must provide a reason for rejection.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Update report status
            report.Status = "Rejected";
            report.RejectionReason = rejectionReason;
            report.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Create notification for pilot
            await CreateNotificationForPilotAsync(report, "Rejected", rejectionReason);

            TempData["SuccessMessage"] = "Report rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Creates a notification for the pilot when their report is processed
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
    }

}
