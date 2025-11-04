using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using newidentitytest.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for hjemmesiden. 
    /// Viser startside, personvernside og feilside.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connectionString;

        /// <summary>
        /// Henter connection string fra konfigurasjonen (appsettings.json).
        /// </summary>
        public HomeController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Tester om vi klarer � koble til databasen. 
        /// Viser en melding p� forsiden om det gikk bra eller ikke.
        /// </summary>
        
        //Krever at brukeren er autentisert for å se siden.
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // rett brukere mot kartet først for å registrere hinder.
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("DataForm", "Obstacle");
            }

            string successMessage = "Connected to MariaDB successfully!";
            string errorMessage = "Failed to connect to MariaDB.";

            try
            {
                // Pr�ver � �pne en kobling til databasen
                await using var conn = new MySqlConnection(connectionString);
                await conn.OpenAsync();

                // Hvis det g�r bra vises en suksessmelding
                return View("Index", successMessage);
            }
            catch (Exception ex)
            {
                // Hvis noe g�r galt, vis en feilmelding
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
        /// Viser feilsiden med informasjon om RequestId (nyttig for feils�king).
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
