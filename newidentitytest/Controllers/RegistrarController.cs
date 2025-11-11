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
		public async Task<IActionResult> Index(string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
		{
			// Base query
			var query = _db.Reports.AsQueryable();

			// Search across key fields
			if (!string.IsNullOrWhiteSpace(search))
			{
				var s = search.ToLower();
				query = query.Where(r =>
					r.Id.ToString().Contains(search) ||
					(r.ObstacleType != null && r.ObstacleType.ToLower().Contains(s)) ||
					(r.ObstacleDescription != null && r.ObstacleDescription.ToLower().Contains(s)) ||
					(r.ObstacleLocation != null && r.ObstacleLocation.ToLower().Contains(s))
				);
			}

			// Sorting
			switch ((sortBy ?? "").ToLower())
			{
				case "id":
					query = sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id);
					break;
				case "type":
					query = sortOrder == "asc" ? query.OrderBy(r => r.ObstacleType ?? "") : query.OrderByDescending(r => r.ObstacleType ?? "");
					break;
				case "description":
					query = sortOrder == "asc" ? query.OrderBy(r => r.ObstacleDescription ?? "") : query.OrderByDescending(r => r.ObstacleDescription ?? "");
					break;
				case "location":
					query = sortOrder == "asc" ? query.OrderBy(r => r.ObstacleLocation ?? "") : query.OrderByDescending(r => r.ObstacleLocation ?? "");
					break;
				case "createdat":
				default:
					query = sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt);
					break;
			}

			var reports = await query.ToListAsync();

			ViewBag.SortBy = sortBy;
			ViewBag.SortOrder = sortOrder;
			ViewBag.Search = search;

			return View(reports);
		}
	}
}
