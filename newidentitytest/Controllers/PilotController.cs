using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;

namespace newidentitytest.Controllers
{
	[Authorize(Roles = "Pilot")]
	public class PilotController : Controller
	{
		private readonly ApplicationDbContext _db;

		public PilotController(ApplicationDbContext db)
		{
			_db = db;
		}

		// GET: /Pilot/Index
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Forbid();
			}

			// Get count of user's reports
			var myReportsCount = await _db.Reports
				.Where(r => r.UserId == userId)
				.CountAsync();
			ViewBag.MyReportsCount = myReportsCount;

			// Get count of user's drafts
			var myDraftsCount = await _db.Reports
				.Where(r => r.UserId == userId && r.Status == "Draft")
				.CountAsync();
			ViewBag.MyDraftsCount = myDraftsCount;

			// Get count of submitted reports (non-drafts)
			var submittedCount = await _db.Reports
				.Where(r => r.UserId == userId && r.Status != "Draft")
				.CountAsync();
			ViewBag.SubmittedReportsCount = submittedCount;

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

		// GET: /Pilot/MyReports
		[HttpGet]
		public async Task<IActionResult> MyReports(string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Forbid();
			}

			// Build query for user's reports only
			var query = from r in _db.Reports
						where r.UserId == userId
						join u in _db.Users on r.UserId equals u.Id into gj
						from u in gj.DefaultIfEmpty()
						select new newidentitytest.Models.ReportListItem
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
					(r.ObstacleType != null && r.ObstacleType.ToLower().Contains(searchLower)) ||
					r.CreatedAt.ToString("MMM dd, yyyy").ToLower().Contains(searchLower)
				);
			}

			// Apply sorting
			query = sortBy.ToLower() switch
			{
				"id" => sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id),
				"createdat" => sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
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
	}
}
