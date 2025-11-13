using Microsoft.AspNetCore.Mvc;
using newidentitytest.Models;
using newidentitytest.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for hjemmesiden. 
    /// Viser startside, personvernside og feilside.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Injects ApplicationDbContext for database operations.
        /// </summary>
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tester om vi klarer å koble til databasen. 
        /// Viser en melding på forsiden om det gikk bra eller ikke.
        /// </summary>
        
        //Krever at brukeren er autentisert for å se siden.
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Redirect registrars to their landing page
            if (User.IsInRole("Registrar"))
            {
                return RedirectToAction("Index", "Registrar");
            }
            
            // Redirect pilots to their landing page
            if (User.IsInRole("Pilot"))
            {
                return RedirectToAction("Index", "Pilot");
            }
            
            // Redirect non-admin users to the obstacle form
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("DataForm", "Obstacle");
            }

            string successMessage = "Connected to MariaDB successfully!";
            string errorMessage = "Failed to connect to MariaDB.";

            try
            {
                // Test database connection using EF Core
                bool canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // Hvis det går bra vises en suksessmelding
                    return View("Index", successMessage);
                }
                else
                {
                    return View("Index", errorMessage + " Database is not available.");
                }
            }
            catch (Exception ex)
            {
                // Hvis noe går galt, vis en feilmelding
                _logger.LogError(ex, "Database connection test failed");
                return View("Index", errorMessage + " " + ex.Message);
            }
        }

        /// <summary>
        /// Viser personvernsiden.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Viser feilsiden med informasjon om RequestId (nyttig for feilsøking).
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
