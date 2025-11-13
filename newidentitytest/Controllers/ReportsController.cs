using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                            ObstacleLocation = r.ObstacleLocation
                        };

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(r => 
                    r.Id.ToString().Contains(searchLower) ||
                    r.Sender.ToLower().Contains(searchLower) ||
                    (r.ObstacleType != null && r.ObstacleType.ToLower().Contains(searchLower)) ||
                    r.CreatedAt.ToString("MMM dd, yyyy").ToLower().Contains(searchLower)
                );
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "id" => sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id),
                "createdat" => sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
                "sender" => sortOrder == "asc" ? query.OrderBy(r => r.Sender) : query.OrderByDescending(r => r.Sender),
                "obstacletype" => sortOrder == "asc" ? query.OrderBy(r => r.ObstacleType ?? "") : query.OrderByDescending(r => r.ObstacleType ?? ""),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var items = await query.ToListAsync();
            
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


        // GET /api/reports/approved
        [HttpGet("/api/reports/approved")]
        [AllowAnonymous]  // Public endpoint - anyone can see approved reports
        public async Task<IActionResult> GetApproved()
        {
            var approvedReports = await _db.Reports
                .Where(r => r.Status == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.ObstacleType,
                    r.ObstacleHeight,
                    r.ObstacleDescription,
                    r.ObstacleLocation,
                    r.CreatedAt
                })
                .ToListAsync();
            
            return Ok(approvedReports);
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

            TempData["SuccessMessage"] = "Report rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

}
