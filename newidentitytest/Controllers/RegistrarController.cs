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
			// Get count for display purposes
			var reportCount = await _db.Reports.CountAsync();
			ViewBag.ReportCount = reportCount;
			
			// Pending reports count (placeholder: all reports are pending until approval/rejection system is implemented)
			var pendingCount = await _db.Reports.CountAsync();
			ViewBag.PendingReportsCount = pendingCount;
			
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
	}
}
