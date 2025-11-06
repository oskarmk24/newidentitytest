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
        public async Task<IActionResult> Index()
        {
            // Build a simple list view model by joining reports with Identity users
            // - Left join (DefaultIfEmpty) so reports without a user still show
            // - Newest first
            var items = await (from r in _db.Reports
                               join u in _db.Users on r.UserId equals u.Id into gj
                               from u in gj.DefaultIfEmpty()
                               orderby r.CreatedAt descending
                               select new ReportListItem
                               {
                                   Id = r.Id,
                                   CreatedAt = r.CreatedAt,
                                   Sender = u != null ? (u.Email ?? u.UserName) : "(unknown)",
                                   ObstacleType = r.ObstacleType,
                                   ObstacleLocation = r.ObstacleLocation
                               }).ToListAsync();
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
    }
}
