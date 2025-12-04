using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Controllers;
using newidentitytest.Data;
using newidentitytest.Models;
using Xunit;

namespace newidentitytest.Tests
{
    public class PilotControllerTests
    {
        // Denne testen dekker den kritiske tilgangskontroll-grenen: MyReports returnerer kun rapporter for den innloggede piloten.
        // Hva: sikrer at MyReports kun viser rapporter som tilhører den autentiserte brukeren.
        // Hvorfor: forhindrer at piloter ser andre piloters rapporter.
        // Hvordan: seed rapporter for user-1 og user-2, kall MyReports som user-1, assert at kun user-1s rapporter returneres.
        [Fact]
        public async Task MyReports_ReturnsOnlyCurrentUsersReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Pilot_MyReports_Isolation")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter for forskjellige brukere
            db.Reports.Add(new Report
            {
                UserId = "pilot-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                ObstacleType = "Building"
            });
            db.Reports.Add(new Report
            {
                UserId = "pilot-2",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}",
                ObstacleType = "Tower"
            });
            db.Reports.Add(new Report
            {
                UserId = "pilot-1",
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}",
                ObstacleType = "Antenna"
            });
            await db.SaveChangesAsync();

            var controller = new PilotController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "pilot-1"),
                        new Claim(ClaimTypes.Role, "Pilot")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.MyReports();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(2, reports.Count);
            Assert.All(reports, r => Assert.True(r.Id == 1 || r.Id == 3)); // Kun pilot-1s rapporter
        }

        // Denne testen dekker den kritiske grenen: Index returnerer Forbid når userId mangler.
        // Hva: sikrer at Index returnerer Forbid hvis brukeren ikke har gyldig userId.
        // Hvorfor: forhindrer feil ved manglende brukerinformasjon.
        // Hvordan: kall Index uten userId claim, forvent ForbidResult.
        [Fact]
        public async Task Index_NoUserId_ReturnsForbid()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Pilot_Index_NoUserId")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new PilotController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // Ingen claims
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Index beregner korrekt statistikk for pilotens rapporter.
        // Hva: sikrer at Index beregner og viser korrekt antall rapporter, utkast og innsendte rapporter.
        // Hvorfor: dashboardet må vise korrekt informasjon for piloten.
        // Hvordan: seed rapporter med forskjellige statuser, kall Index, assert at ViewBag-verdiene er korrekte.
        [Fact]
        public async Task Index_CalculatesCorrectStatistics()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Pilot_Index_Statistics")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter med forskjellige statuser
            db.Reports.Add(new Report
            {
                UserId = "pilot-1",
                Status = "Draft",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "pilot-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "pilot-1",
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new PilotController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "pilot-1"),
                        new Claim(ClaimTypes.Role, "Pilot")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(3, viewResult.ViewData["MyReportsCount"]); // Totalt antall
            Assert.Equal(1, viewResult.ViewData["MyDraftsCount"]); // Kun Draft
            Assert.Equal(2, viewResult.ViewData["SubmittedReportsCount"]); // Pending + Approved
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: MarkNotificationAsRead kan kun markere notifikasjoner som tilhører brukeren.
        // Hva: sikrer at MarkNotificationAsRead ikke kan markere notifikasjoner som tilhører andre brukere.
        // Hvorfor: forhindrer at brukere kan endre andres notifikasjoner.
        // Hvordan: seed notifikasjon eid av user-1, kall MarkNotificationAsRead som user-2, assert at notifikasjonen ikke endres.
        [Fact]
        public async Task MarkNotificationAsRead_OtherUsersNotification_DoesNotMarkAsRead()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Pilot_MarkNotification_Isolation")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett notifikasjon for user-1
            db.Notifications.Add(new Notification
            {
                Id = 1,
                UserId = "pilot-1",
                ReportId = 1,
                Title = "Test Notification",
                Message = "Test",
                IsRead = false
            });
            await db.SaveChangesAsync();

            var controller = new PilotController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "pilot-2"), // Annen bruker
                        new Claim(ClaimTypes.Role, "Pilot")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.MarkNotificationAsRead(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            // Sjekk at notifikasjonen ikke er markert som lest
            var notification = await db.Notifications.FindAsync(1);
            Assert.NotNull(notification);
            Assert.False(notification.IsRead);
        }

        // Denne testen dekker den kritiske grenen: MarkAllNotificationsAsRead markerer kun notifikasjoner for den innloggede brukeren.
        // Hva: sikrer at MarkAllNotificationsAsRead kun markerer notifikasjoner som tilhører den autentiserte brukeren.
        // Hvorfor: forhindrer at brukere kan endre andres notifikasjoner.
        // Hvordan: seed notifikasjoner for user-1 og user-2, kall MarkAllNotificationsAsRead som user-1, assert at kun user-1s notifikasjoner markeres.
        [Fact]
        public async Task MarkAllNotificationsAsRead_OnlyMarksCurrentUsersNotifications()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Pilot_MarkAll_Isolation")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett notifikasjoner for forskjellige brukere
            db.Notifications.Add(new Notification
            {
                UserId = "pilot-1",
                ReportId = 1,
                Title = "Notification 1",
                Message = "Test 1",
                IsRead = false
            });
            db.Notifications.Add(new Notification
            {
                UserId = "pilot-2",
                ReportId = 2,
                Title = "Notification 2",
                Message = "Test 2",
                IsRead = false
            });
            db.Notifications.Add(new Notification
            {
                UserId = "pilot-1",
                ReportId = 3,
                Title = "Notification 3",
                Message = "Test 3",
                IsRead = false
            });
            await db.SaveChangesAsync();

            var controller = new PilotController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "pilot-1"),
                        new Claim(ClaimTypes.Role, "Pilot")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.MarkAllNotificationsAsRead();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            // Sjekk at kun pilot-1s notifikasjoner er markert som lest
            var pilot1Notifications = await db.Notifications
                .Where(n => n.UserId == "pilot-1")
                .ToListAsync();
            Assert.All(pilot1Notifications, n => Assert.True(n.IsRead));
            
            // Sjekk at pilot-2s notifikasjon ikke er markert
            var pilot2Notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.UserId == "pilot-2");
            Assert.NotNull(pilot2Notification);
            Assert.False(pilot2Notification.IsRead);
        }
    }
}
