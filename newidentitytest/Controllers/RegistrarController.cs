using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
	/// <summary>
	/// Controller for registerførere (Registrar-rolle).
	/// Gir dashboard med oversikt over rapporter og tilgang til ventende rapporter med sortering og søk.
	/// Viser statistikk over totale rapporter, ventende rapporter og egne tildelte saker.
	/// Krever Registrar-rolle.
	/// </summary>
	[Authorize(Roles = "Registrar")]
	public class RegistrarController : Controller
	{
		private readonly ApplicationDbContext _db;

		/// <summary>
		/// Initialiserer controlleren med ApplicationDbContext for databaseoperasjoner.
		/// </summary>
		public RegistrarController(ApplicationDbContext db)
		{
			_db = db;
		}

		/// <summary>
		/// Viser dashboard for registerføreren med statistikk og oversikt over tildelte saker.
		/// Beregner og viser:
		/// - Totalt antall rapporter i systemet
		/// - Antall ventende rapporter (Pending)
		/// - De 5 nyeste tildelte sakene til den innloggede registerføreren (sortert etter opprettelsesdato)
		/// - Systemstatus (databaseforbindelse)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			// Hent totalt antall rapporter for visning i dashboard
			var reportCount = await _db.Reports.CountAsync();
			ViewBag.ReportCount = reportCount;

			// Hent antall ventende rapporter (effektiv tell-spørring)
			var pendingCount = await _db.Reports.CountAsync(r => r.Status == "Pending");
			ViewBag.PendingReportsCount = pendingCount;
			
			// Hent de 5 nyeste sakene tildelt den innloggede registerføreren
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
			
			// Sjekk systemstatus ved å teste databaseforbindelse
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

		/// <summary>
		/// Viser liste over alle ventende rapporter (Pending) med sortering og søkefunksjonalitet.
		/// Inkluderer informasjon om avsender og organisasjon via joins.
		/// Støtter sortering etter: id, CreatedAt, Sender, OrganizationName, ObstacleType, Status (standard: CreatedAt desc).
		/// Støtter søk i: Id, Sender, OrganizationName, ObstacleType, CreatedAt (dato), Status.
		/// Søkefilteret anvendes i minnet for å unngå EF Core oversettelsesproblemer med komplekse strengoperasjoner.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Pending(string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
		{
			// Bygg spørring med joins for å hente bruker- og organisasjonsinformasjon
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

			// Anvend sortering basert på sortBy-parameteren
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

			// Anvend søkefilter i minnet for å unngå EF Core oversettelsesproblemer med komplekse strengoperasjoner
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
