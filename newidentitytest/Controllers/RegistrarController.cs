using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
		public IActionResult Index()
		{
			// Load recent reports to show on registrar dashboard
			var reports = _db.Reports
				.OrderByDescending(r => r.CreatedAt)
				.Take(10)
				.ToList();

			return View(reports);
		}
	}
}
