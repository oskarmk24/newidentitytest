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
		public async Task<IActionResult> Pending(string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
		{
			// Build query with joins to get user and organization info
			var query = from r in _db.Reports
						where r.Status == "Pending"
						join u in _db.Users on r.UserId equals u.Id into userJoin
						from u in userJoin.DefaultIfEmpty()
						join o in _db.Organizations on u.OrganizationId equals o.Id into orgJoin
						from o in orgJoin.DefaultIfEmpty()
						select new Models.ReportListItem
						{
							Id = r.Id,
							CreatedAt = r.CreatedAt,
							Sender = u != null ? (u.Email ?? u.UserName) : "(unknown)",
							OrganizationName = o != null ? o.Name : null,
							ObstacleType = r.ObstacleType,
							Status = r.Status,
							ObstacleLocation = r.ObstacleLocation
						};

			// Apply sorting
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

			// Apply search filter in-memory to avoid EF translation issues
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

			// Pass sorting info to view
			ViewBag.SortBy = sortBy;
			ViewBag.SortOrder = sortOrder;
			ViewBag.Search = search;

			return View(items);
		}
	}
}
