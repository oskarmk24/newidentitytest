using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller som håndterer skjema for å registrere hinder.
    /// </summary>
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
        public async Task<IActionResult> DataForm(ObstacleData obstacleData)
        {
            // Sjekker at modellen er gyldig (validering fra ObstacleData)
            if (!ModelState.IsValid)
            {
                return View(obstacleData);
            }

            try
            {
                var report = new Report
                {
                    ObstacleName = obstacleData.ObstacleName,
                    ObstacleHeight = Convert.ToInt32(Math.Round(obstacleData.ObstacleHeight)),
                    ObstacleDescription = obstacleData.ObstacleDescription,
                    ObstacleLocation = obstacleData.ObstacleLocation
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
    }
}
