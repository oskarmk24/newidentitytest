using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
	[Authorize(Roles = "Registrar")]
	public class RegistrarController : Controller
	{
		private readonly ApplicationDbContext _db;

		public RegistrarController(ApplicationDbContext db)
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

			// Pending reports count (shallow query)
			var pendingCount = await _db.Reports.CountAsync(r => r.Status == "Pending");
			ViewBag.PendingReportsCount = pendingCount;
			
			// Simple "My cases" list for the logged-in registrar
			var registrarId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			List<Report> myCases = new();
			if (!string.IsNullOrEmpty(registrarId))
			{
				myCases = await _db.Reports
					.Where(r => r.AssignedRegistrarId == registrarId && r.Status == "Pending")
					.OrderByDescending(r => r.CreatedAt)
					.Take(5)
					.ToListAsync();
			}
			ViewBag.MyAssignedReports = myCases;
			
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
