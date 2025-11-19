using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    [Authorize(Roles = "OrganizationManager")]
    public class OrganizationManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrganizationManagerController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /OrganizationManager/Index
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

            // Get user IDs for this organization
            var organizationUserIds = await _db.Users
                .Where(u => u.OrganizationId == organization.Id)
                .Select(u => u.Id)
                .ToListAsync();

            // Get statistics
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

