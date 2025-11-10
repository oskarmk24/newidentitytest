using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;
using Microsoft.AspNetCore.Authorization; // Autorisasjon for å kreve innlogging
using System.Security.Claims; // Gir tilgang til brukerens Claims (f.eks. NameIdentifier)

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller som håndterer skjema for å registrere hinder.
    /// </summary>
    [Authorize] // Krev at brukeren er innlogget for å bruke disse endepunktene
    public class ObstacleController : Controller
    {
        // Repository brukes til å lagre data i databasen
        private readonly ApplicationDbContext _dbContext;

        public ObstacleController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Viser skjemaet for å registrere et nytt hinder (GET).
        /// </summary>
        [HttpGet]
        public ActionResult DataForm()
        {
            return View();
        }

        /// <summary>
        /// Tar imot skjema-data (POST).
        /// Hvis data er gyldige lagres de i databasen og vi viser en oversiktsside.
        /// Hvis noe er feil, vises skjemaet på nytt med feilmeldinger.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(ObstacleData obstacleData)
        {
            // Sjekker at modellen er gyldig (validering fra ObstacleData)
            if (!ModelState.IsValid)
            {
                return View(obstacleData);
            }

            // Hent innlogget brukers ID fra Claims.
            // ASP.NET Core Identity utsteder en Claim av type ClaimTypes.NameIdentifier som er primærnøkkelen (AspNetUsers.Id).
            // Vi lagrer denne som UserId på Report for å kunne filtrere rapporter pr. bruker senere.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Dersom bruker på en eller annen måte ikke er autentisert (til tross for [Authorize])
                // returnerer vi Forbid for å signalisere manglende tilgang.
                return Forbid();
            }

            try
            {

                var report = new Report
                {
                    ObstacleHeight = Convert.ToInt32(Math.Round(obstacleData.ObstacleHeight)),
                    ObstacleDescription = obstacleData.ObstacleDescription,
                    ObstacleLocation = obstacleData.ObstacleLocation,
                    ObstacleType = obstacleData.ObstacleType,
                    UserId = userId
                };

                // Lagrer data i databasen via EF Core
                _dbContext.Reports.Add(report);
                await _dbContext.SaveChangesAsync();
                ViewBag.SavedReportId = report.Id;
                TempData["Success"] = "Report saved with ID " + report.Id;
            }
            catch (DbUpdateException ex)
            {
                // Viser en feilmelding hvis databasen ikke svarer
                ModelState.AddModelError(string.Empty, "Database error: " + ex.Message);
                return View(obstacleData);
            }

            // Viser oversiktssiden med dataene etter at de er lagret
            return View("Overview", obstacleData);
        }

        [HttpGet]
        [Route("/api/obstacles")]
        public async Task<IActionResult> GetObstaclesForMap()
        {
            var reports = await _dbContext.Reports
                .Where(r => !string.IsNullOrEmpty(r.ObstacleLocation))
                .Select(r => new
                {
                    Id = r.Id,
                    Type = r.ObstacleType ?? "Unknown",
                    Height = r.ObstacleHeight,
                    Location = r.ObstacleLocation
                })
                .ToListAsync();
            
            return Ok(reports);
        }
    }
}
