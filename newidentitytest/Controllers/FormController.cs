using Microsoft.AspNetCore.Mvc;

namespace newidentitytest.Controllers
{
    public class FormController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
