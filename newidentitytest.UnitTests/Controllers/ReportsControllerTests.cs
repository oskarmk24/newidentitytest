using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using newidentitytest.Controllers;
using newidentitytest.Data;
using newidentitytest.Models;
using Xunit;

namespace newidentitytest.Tests
{
    public class ReportsControllerTests
    {
        // Denne testen dekker den kritiske grenen: Approve oppdaterer rapportstatus til "Approved" og oppretter notifikasjon.
        // Hva: sikrer at Approve oppdaterer rapporten og oppretter notifikasjon til piloten.
        // Hvorfor: dette er en kritisk forretningsprosess - godkjenning av rapporter.
        // Hvordan: seed Pending-rapport, kall Approve som Registrar, assert at status er "Approved" og notifikasjon opprettes.
        [Fact]
        public async Task Approve_UpdatesReportStatusAndCreatesNotification()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Reports_Approve_Notification")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapport
            db.Reports.Add(new Report
            {
                Id = 1,
                UserId = "pilot-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                ObstacleType = "Building"
            });
            
            // Opprett registerfører-bruker
            db.Users.Add(new ApplicationUser
            {
                Id = "registrar-1",
                Email = "registrar@test.com",
                UserName = "registrar@test.com"
            });
            await db.SaveChangesAsync();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new ReportsController(db, userManagerMock.Object);
            
            // Sett opp TempData
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "registrar-1"),
                new Claim(ClaimTypes.Role, "Registrar")
            }, "TestAuth"));

            // Act
            var result = await controller.Approve(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            
            var report = await db.Reports.FindAsync(1);
            Assert.NotNull(report);
            Assert.Equal("Approved", report.Status);
            Assert.NotNull(report.ProcessedAt);
            Assert.Null(report.RejectionReason);
            
            // Sjekk at notifikasjon er opprettet
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.ReportId == 1 && n.UserId == "pilot-1");
            Assert.NotNull(notification);
            Assert.Contains("approved", notification.Title, StringComparison.OrdinalIgnoreCase);
        }

        // Denne testen dekker den kritiske grenen: Reject krever rejectionReason og oppretter notifikasjon.
        // Hva: sikrer at Reject validerer rejectionReason og oppretter notifikasjon med begrunnelse.
        // Hvorfor: avslag må ha begrunnelse, og piloten må informeres.
        // Hvordan: seed Pending-rapport, kall Reject med rejectionReason, assert at status er "Rejected" og notifikasjon inneholder begrunnelse.
        [Fact]
        public async Task Reject_WithReason_UpdatesStatusAndCreatesNotification()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Reports_Reject_Notification")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            db.Reports.Add(new Report
            {
                Id = 1,
                UserId = "pilot-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            
            db.Users.Add(new ApplicationUser
            {
                Id = "registrar-1",
                Email = "registrar@test.com",
                UserName = "registrar@test.com"
            });
            await db.SaveChangesAsync();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new ReportsController(db, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "registrar-1"),
                new Claim(ClaimTypes.Role, "Registrar")
            }, "TestAuth"));

            // Act
            var result = await controller.Reject(1, "Insufficient information");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            
            var report = await db.Reports.FindAsync(1);
            Assert.NotNull(report);
            Assert.Equal("Rejected", report.Status);
            Assert.Equal("Insufficient information", report.RejectionReason);
            Assert.NotNull(report.ProcessedAt);
            
            // Sjekk at notifikasjon inneholder begrunnelse
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.ReportId == 1 && n.UserId == "pilot-1");
            Assert.NotNull(notification);
            Assert.Contains("rejected", notification.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Insufficient information", notification.Message);
        }

        // Denne testen dekker den kritiske grenen: Reject returnerer feilmelding når rejectionReason mangler.
        // Hva: sikrer at Reject validerer at rejectionReason er oppgitt.
        // Hvorfor: avslag må alltid ha en begrunnelse.
        // Hvordan: seed Pending-rapport, kall Reject uten rejectionReason, assert at feilmelding vises og status ikke endres.
        [Fact]
        public async Task Reject_WithoutReason_ReturnsErrorAndDoesNotUpdateStatus()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Reports_Reject_NoReason")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            db.Reports.Add(new Report
            {
                Id = 1,
                UserId = "pilot-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            await db.SaveChangesAsync();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new ReportsController(db, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "registrar-1"),
                new Claim(ClaimTypes.Role, "Registrar")
            }, "TestAuth"));

            // Act
            var result = await controller.Reject(1, "");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            
            // Sjekk at rapporten ikke er endret
            var report = await db.Reports.FindAsync(1);
            Assert.NotNull(report);
            Assert.Equal("Pending", report.Status); // Status skal ikke endres
            Assert.Null(report.RejectionReason);
        }

        // Denne testen dekker den kritiske grenen: Delete sletter rapport og tilknyttede notifikasjoner, oppretter notifikasjon til piloten.
        // Hva: sikrer at Delete sletter rapporten, alle tilknyttede notifikasjoner, og oppretter ny notifikasjon om sletting.
        // Hvorfor: dette er en kritisk operasjon som må håndteres korrekt.
        // Hvordan: seed rapport med notifikasjoner, kall Delete, assert at rapport og notifikasjoner slettes, og ny notifikasjon opprettes.
        [Fact]
        public async Task Delete_RemovesReportAndNotificationsAndCreatesDeletionNotification()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Reports_Delete_Cleanup")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            db.Reports.Add(new Report
            {
                Id = 1,
                UserId = "pilot-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            
            // Opprett eksisterende notifikasjoner for rapporten
            db.Notifications.Add(new Notification
            {
                UserId = "pilot-1",
                ReportId = 1,
                Title = "Old notification",
                Message = "Test"
            });
            
            db.Users.Add(new ApplicationUser
            {
                Id = "registrar-1",
                Email = "registrar@test.com",
                UserName = "registrar@test.com"
            });
            await db.SaveChangesAsync();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new ReportsController(db, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "registrar-1"),
                new Claim(ClaimTypes.Role, "Registrar")
            }, "TestAuth"));

            // Act
            var result = await controller.Delete(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            // Sjekk at rapporten er slettet
            var report = await db.Reports.FindAsync(1);
            Assert.Null(report);
            
            // Sjekk at gamle notifikasjoner er slettet
            var oldNotifications = await db.Notifications
                .Where(n => n.ReportId == 1 && n.Title == "Old notification")
                .ToListAsync();
            Assert.Empty(oldNotifications);
            
            // Sjekk at ny notifikasjon om sletting er opprettet
            var deletionNotification = await db.Notifications
                .FirstOrDefaultAsync(n => n.UserId == "pilot-1" && n.Title.Contains("deleted"));
            Assert.NotNull(deletionNotification);
        }

        // Denne testen dekker den kritiske grenen: Details returnerer NotFound når rapport ikke finnes.
        // Hva: sikrer at Details håndterer manglende rapporter korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: kall Details med ikke-eksisterende ID, forvent NotFoundResult.
        [Fact]
        public async Task Details_NonExistentReport_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Reports_Details_NotFound")
                .Options;

            await using var db = new ApplicationDbContext(options);

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new ReportsController(db, userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: AssignRegistrar validerer at registrarId er gyldig Registrar.
        // Hva: sikrer at AssignRegistrar kun tillater tildeling til brukere med Registrar-rolle.
        // Hvorfor: forhindrer at ikke-Registrar brukere tildeles rapporter.
        // Hvordan: seed rapport og bruker uten Registrar-rolle, kall AssignRegistrar, assert at tildeling feiler.
        [Fact]
        public async Task AssignRegistrar_InvalidRegistrarId_ReturnsError()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Reports_Assign_Invalid")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            db.Reports.Add(new Report
            {
                Id = 1,
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            
            // Opprett bruker uten Registrar-rolle
            db.Users.Add(new ApplicationUser
            {
                Id = "pilot-1",
                Email = "pilot@test.com",
                UserName = "pilot@test.com"
            });
            await db.SaveChangesAsync();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            // Mock IsInRoleAsync til å returnere false (ikke Registrar)
            userManagerMock.Setup(m => m.IsInRoleAsync(
                It.Is<ApplicationUser>(u => u.Id == "pilot-1"),
                "Registrar"))
                .ReturnsAsync(false);

            var controller = new ReportsController(db, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "registrar-1"),
                new Claim(ClaimTypes.Role, "Registrar")
            }, "TestAuth"));

            // Act
            var result = await controller.AssignRegistrar(1, "pilot-1");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            
            // Sjekk at rapporten ikke er tildelt
            var report = await db.Reports.FindAsync(1);
            Assert.NotNull(report);
            Assert.Null(report.AssignedRegistrarId);
        }
    }
}
