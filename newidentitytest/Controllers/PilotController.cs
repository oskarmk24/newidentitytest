using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

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

			// Get unread notifications
			var unreadNotifications = await _db.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.OrderByDescending(n => n.CreatedAt)
				.Take(10)
				.ToListAsync();
			ViewBag.UnreadNotifications = unreadNotifications;
			ViewBag.UnreadNotificationsCount = unreadNotifications.Count;

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
                            Status = r.Status,
							ObstacleLocation = r.ObstacleLocation
						};

			// Apply sorting
			query = sortBy.ToLower() switch
			{
				"id" => sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id),
				"createdat" => sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
				"obstacletype" => sortOrder == "asc" ? query.OrderBy(r => r.ObstacleType ?? "") : query.OrderByDescending(r => r.ObstacleType ?? ""),
				"status" => sortOrder == "asc" ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
				_ => query.OrderByDescending(r => r.CreatedAt)
			};

			var items = await query.ToListAsync();

			// Apply search filter in-memory so string operations won't break SQL translation
			if (!string.IsNullOrWhiteSpace(search))
			{
				var searchLower = search.ToLowerInvariant();
				items = items.Where(r =>
						r.Id.ToString().Contains(searchLower) ||
						((r.ObstacleType ?? string.Empty).ToLowerInvariant().Contains(searchLower)) ||
						r.CreatedAt.ToString("MMM dd, yyyy").ToLowerInvariant().Contains(searchLower) ||
						((r.Status ?? string.Empty).ToLowerInvariant().Contains(searchLower)) ||
						((r.ObstacleLocation ?? string.Empty).ToLowerInvariant().Contains(searchLower))
					)
					.ToList();
			}

			// Pass sorting info to view
			ViewBag.SortBy = sortBy;
			ViewBag.SortOrder = sortOrder;
			ViewBag.Search = search;

			return View(items);
		}

		// Mark notification as read
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MarkNotificationAsRead(int id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Forbid();
			}

			var notification = await _db.Notifications
				.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

			if (notification != null && !notification.IsRead)
			{
				notification.IsRead = true;
				notification.ReadAt = DateTime.UtcNow;
				await _db.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		// Mark all notifications as read
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MarkAllNotificationsAsRead()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Forbid();
			}

			var unreadNotifications = await _db.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.ToListAsync();

			foreach (var notification in unreadNotifications)
			{
				notification.IsRead = true;
				notification.ReadAt = DateTime.UtcNow;
			}

			await _db.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}
	}
}
