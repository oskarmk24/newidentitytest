using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Controllers;
using newidentitytest.Data;
using newidentitytest.Models;
using Xunit;

namespace newidentitytest.Tests
{
    // Mock for TempDataProvider til bruk i tester
    public class MockTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return _data;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            foreach (var item in values)
            {
                _data[item.Key] = item.Value;
            }
        }
    }

    public class ObstacleControllerTests
    {
        // Denne testen dekker den kritiske tilgangskontroll-grenen: brukere kan ikke redigere utkast som tilhører andre brukere.
        // Hva: sikrer at EditDraft returnerer NotFound når en bruker prøver å redigere en annen brukers utkast.
        // Hvorfor: forhindrer uautorisert tilgang til andre brukeres utkast-rapporter.
        // Hvordan: seed utkast eid av user-1, kall EditDraft som user-2, forvent NotFoundResult.
        [Fact]
        public async Task EditDraft_UserTriesToEditOtherUsersDraft_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Obstacle_EditDraft_Forbid")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett et utkast eid av user-1
            db.Reports.Add(new Report
            {
                Id = 1,
                UserId = "user-1",
                Status = "Draft",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new ObstacleController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-2") // Annen bruker
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.EditDraft(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: Drafts viser kun utkast for den innloggede brukeren.
        // Hva: sikrer at Drafts returnerer kun utkast som tilhører den autentiserte brukeren.
        // Hvorfor: forhindrer at brukere ser andre brukeres utkast-rapporter.
        // Hvordan: seed utkast for user-1 og user-2, kall Drafts som user-1, assert at kun user-1s utkast returneres.
        [Fact]
        public async Task Drafts_ReturnsOnlyCurrentUsersDrafts()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Obstacle_Drafts_Isolation")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett utkast for forskjellige brukere
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Draft",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "user-2",
                Status = "Draft",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Draft",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new ObstacleController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-1"),
                        new Claim(ClaimTypes.Role, "Pilot")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Drafts();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var drafts = Assert.IsAssignableFrom<List<Report>>(viewResult.Model);
            Assert.Equal(2, drafts.Count);
            Assert.All(drafts, d => Assert.Equal("user-1", d.UserId));
        }

        // Denne testen dekker den kritiske grenen: DataForm POST med action="submit" oppretter en ny rapport med status "Pending".
        // Hva: sikrer at innsending av et komplett skjema oppretter en Pending-rapport.
        // Hvorfor: dette er hovedarbeidsflyten for piloter som sender inn hinderrapporter.
        // Hvordan: opprett gyldig ObstacleData, kall DataForm med action="submit", assert at rapport opprettes med status "Pending".
        [Fact]
        public async Task DataForm_SubmitAction_CreatesPendingReport()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Obstacle_Submit_Report")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new ObstacleController(db);
            
            // Sett opp TempData med enkel mock
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "pilot-1")
            }, "TestAuth"));

            var obstacleData = new ObstacleData
            {
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                ObstacleType = "Building",
                ObstacleHeight = 50,
                ObstacleDescription = "Tall building"
            };

            // Act
            var result = await controller.DataForm(obstacleData, "submit");

            // Assert
            var report = await db.Reports.FirstOrDefaultAsync(r => r.UserId == "pilot-1");
            Assert.NotNull(report);
            Assert.Equal("Pending", report.Status);
            Assert.Equal("Building", report.ObstacleType);
            Assert.Equal(50, report.ObstacleHeight);
        }

        // Denne testen dekker den kritiske grenen: DataForm POST med action="draft" oppretter et utkast med status "Draft".
        // Hva: sikrer at lagring som utkast oppretter en Draft-rapport og redirecter til Drafts.
        // Hvorfor: piloter trenger å lagre ufullstendige rapporter som utkast.
        // Hvordan: opprett ObstacleData med kun lokasjon, kall DataForm med action="draft", assert at Draft opprettes og redirecter til Drafts.
        [Fact]
        public async Task DataForm_DraftAction_CreatesDraftAndRedirectsToDrafts()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Obstacle_Draft_Save")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new ObstacleController(db);
            
            // Sett opp TempData med enkel mock
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "pilot-1")
            }, "TestAuth"));

            var obstacleData = new ObstacleData
            {
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            };

            // Act
            var result = await controller.DataForm(obstacleData, "draft");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Drafts", redirect.ActionName);
            
            var draft = await db.Reports.FirstOrDefaultAsync(r => r.UserId == "pilot-1" && r.Status == "Draft");
            Assert.NotNull(draft);
            Assert.Equal("Draft", draft.Status);
        }

        // Denne testen dekker den kritiske grenen: GetApprovedObstacles tillater anonym tilgang og returnerer kun Approved-rapporter.
        // Hva: sikrer at det offentlige API-endepunktet returnerer kun godkjente hindre uten å kreve autentisering.
        // Hvorfor: dette endepunktet er ment for offentlig kartvisning.
        // Hvordan: seed rapporter med forskjellige statuser, kall GetApprovedObstacles uten autentisering, assert at kun Approved-rapporter returneres.
        [Fact]
        public async Task GetApprovedObstacles_ReturnsOnlyApprovedReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Obstacle_Approved_API")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            db.Reports.Add(new Report
            {
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                ObstacleType = "Building"
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}",
                ObstacleType = "Tower"
            });
            db.Reports.Add(new Report
            {
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}",
                ObstacleType = "Antenna"
            });
            await db.SaveChangesAsync();

            var controller = new ObstacleController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await controller.GetApprovedObstacles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Bruk reflection eller dynamisk type for anonym type
            var obstacles = okResult.Value as System.Collections.IEnumerable;
            Assert.NotNull(obstacles);
            var count = obstacles.Cast<object>().Count();
            Assert.Equal(2, count);
        }
    }
}
