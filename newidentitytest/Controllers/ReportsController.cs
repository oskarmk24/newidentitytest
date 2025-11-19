using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            // Store the injected DbContext for later queries
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
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

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(r => 
                    r.Id.ToString().Contains(searchLower) ||
                    r.Sender.ToLower().Contains(searchLower) ||
                    (r.ObstacleType != null && r.ObstacleType.ToLower().Contains(searchLower)) ||
                    r.CreatedAt.ToString("MMM dd, yyyy").ToLower().Contains(searchLower) ||
                    r.Status.ToLower().Contains(searchLower)
                );
            }

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
    }

}
