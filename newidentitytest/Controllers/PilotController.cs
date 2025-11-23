using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
	/// <summary>
	/// Controller for piloter (Pilot-rolle).
	/// Gir dashboard med oversikt over egne rapporter, utkast, notifikasjoner og systemstatus.
	/// Støtter visning og administrasjon av pilotens egne rapporter med sortering og søk.
	/// Krever Pilot-rolle. Hver pilot kan kun se og administrere sine egne rapporter og notifikasjoner.
	/// </summary>
	[Authorize(Roles = "Pilot")]
	public class PilotController : Controller
	{
		private readonly ApplicationDbContext _db;

		/// <summary>
		/// Initialiserer controlleren med ApplicationDbContext for databaseoperasjoner.
		/// </summary>
		public PilotController(ApplicationDbContext db)
		{
			_db = db;
		}

		/// <summary>
		/// Viser dashboard for piloten med oversikt over egne rapporter, utkast og notifikasjoner.
		/// Beregner og viser:
		/// - Totalt antall rapporter
		/// - Antall utkast (Draft)
		/// - Antall innsendte rapporter (ikke-utkast)
		/// - Systemstatus (databaseforbindelse)
		/// - Ulesste notifikasjoner (maks 10, sortert etter nyeste først)
		/// Returnerer Forbid hvis brukeren ikke har gyldig userId.
		/// </summary>
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

		/// <summary>
		/// Viser liste over pilotens egne rapporter med sortering og søkefunksjonalitet.
		/// Filtrerer automatisk på den innloggede pilotens userId.
		/// Støtter sortering etter: id, CreatedAt, ObstacleType, Status (standard: CreatedAt desc).
		/// Støtter søk i: Id, ObstacleType, CreatedAt (dato), Status.
		/// Returnerer Forbid hvis brukeren ikke har gyldig userId.
		/// </summary>
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

			// Apply search filter if provided
			if (!string.IsNullOrWhiteSpace(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(r =>
					r.Id.ToString().Contains(searchLower) ||
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

		/// <summary>
		/// Marker en spesifikk notifikasjon som lest for den innloggede piloten.
		/// Sjekker at notifikasjonen tilhører piloten før den markeres som lest.
		/// Oppdaterer IsRead til true og setter ReadAt til nåværende tid (UTC).
		/// Redirecter tilbake til Index etter oppdatering.
		/// Returnerer Forbid hvis brukeren ikke har gyldig userId.
		/// </summary>
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

		/// <summary>
		/// Marker alle uleste notifikasjoner som lest for den innloggede piloten.
		/// Oppdaterer alle notifikasjoner hvor IsRead er false til true og setter ReadAt til nåværende tid (UTC).
		/// Redirecter tilbake til Index etter oppdatering.
		/// Returnerer Forbid hvis brukeren ikke har gyldig userId.
		/// </summary>
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
