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
    /// Håndterer startsiden med rollebaserte redirects, personvernside og feilhåndtering.
    /// Index() redirecter brukere til deres rolle-spesifikke dashboards eller tester databaseforbindelse for Admin/Registrar.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Dependency injection av ApplicationDbContext og Logger.
        /// </summary>
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Hovedstartsiden med rollebasert redirect-logikk.
        /// Redirecter Registrar, OrganizationManager og Pilot til deres respektive dashboards.
        /// Redirecter andre autentiserte brukere (som ikke er Admin/Registrar) til rapportskjemaet.
        /// For Admin og Registrar som ikke blir redirectet, tester metoden databaseforbindelsen og viser resultat.
        /// Krever autentisering via [Authorize] attributt.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Redirect registrars to their landing page
            if (User.IsInRole("Registrar"))
            {
                return RedirectToAction("Index", "Registrar");
            }
            
            // Redirect organization managers to their landing page
            if (User.IsInRole("OrganizationManager"))
            {
                return RedirectToAction("Index", "OrganizationManager");
            }
            
            // Redirect pilots to their landing page
            if (User.IsInRole("Pilot"))
            {
                return RedirectToAction("Index", "Pilot");
            }
            
            // Redirect non-admin and non-registrar users to the obstacle form
            if (!User.IsInRole("Admin") && !User.IsInRole("Registrar"))
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
        /// Viser personvernsiden (Privacy Policy).
        /// Tilgjengelig for alle brukere, krever ikke autentisering.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Viser feilsiden med informasjon om RequestId (nyttig for feilsøking).
        /// ResponseCache er deaktivert for å sikre at feilinformasjon alltid er oppdatert.
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
