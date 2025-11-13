using Microsoft.AspNetCore.Mvc;

namespace newidentitytest.Controllers
{
    public class MapController : Controller
    {
        // GET: /Map
        public IActionResult Index()
        {
            return View();
        }
    }
}