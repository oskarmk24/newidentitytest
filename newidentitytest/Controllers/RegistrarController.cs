using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace newidentitytest.Controllers
{
	[Authorize(Roles = "Registrar")]
	public class RegistrarController : Controller
	{
		private readonly newidentitytest.Data.ApplicationDbContext _db;

		public RegistrarController(newidentitytest.Data.ApplicationDbContext db)
		{
			_db = db;
		}

		// GET: /Registrar/Index
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			// Get total reports count for display purposes
			var reportCount = await _db.Reports.CountAsync();
			ViewBag.ReportCount = reportCount;

			// Pending reports list and count
			var pendingReports = await _db.Reports
				.Where(r => r.Status == "Pending")
				.OrderByDescending(r => r.CreatedAt)
				.Take(50)
				.Select(r => new Models.ReportListItem
				{
					Id = r.Id,
					CreatedAt = r.CreatedAt,
					Sender = "(unknown)", // resolved in view if needed
					ObstacleType = r.ObstacleType,
					Status = r.Status,
					ObstacleLocation = r.ObstacleLocation
				})
				.ToListAsync();

			ViewBag.PendingReports = pendingReports;
			ViewBag.PendingReportsCount = pendingReports.Count;
			
			// Check system status (database connectivity)
			bool isSystemHealthy = false;
			try
			{
				isSystemHealthy = await _db.Database.CanConnectAsync();
			}
			catch
			{
				isSystemHealthy = false;
			}
			
			ViewBag.SystemStatus = isSystemHealthy ? "Active" : "Degraded";
			ViewBag.SystemStatusColor = isSystemHealthy ? "green" : "red";
			
			return View();
		}

		// GET: /Registrar/Pending
		[HttpGet]
		public async Task<IActionResult> Pending()
		{
			var pendingReports = await _db.Reports
				.Where(r => r.Status == "Pending")
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new Models.ReportListItem
				{
					Id = r.Id,
					CreatedAt = r.CreatedAt,
					Sender = "(unknown)",
					ObstacleType = r.ObstacleType,
					Status = r.Status,
					ObstacleLocation = r.ObstacleLocation
				})
				.ToListAsync();

			return View(pendingReports);
		}
	}
}
