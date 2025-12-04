using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using newidentitytest.Controllers;
using newidentitytest.Data;
using newidentitytest.Models;
using Xunit;

namespace newidentitytest.Tests
{
    public class HomeControllerTests
    {
        // Denne testen dekker den kritiske grenen: rollebasert redirect for registerførere.
        // Hva: sikrer at Registrar-brukere sendes til sitt dashboard fra Home/Index.
        // Hvorfor: en regresjon her bryter navigasjonen for en hel rolle.
        // Hvordan: opprett controller med in-memory DbContext, legg til falsk Registrar principal, assert redirect action/controller.
        [Fact]
        public async Task Index_RegistrarUser_RedirectsToRegistrarDashboard()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_Registrar_Redirect")
                .Options;
            await using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "registrar-user"),
                new Claim(ClaimTypes.Role, "Registrar")
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Registrar", redirect.ControllerName);
        }

        // Denne testen dekker den kritiske grenen: rollebasert redirect for organisasjonsledere.
        // Hva: sikrer at OrganizationManager-brukere sendes til sitt dashboard fra Home/Index.
        // Hvorfor: en regresjon her bryter navigasjonen for en hel rolle.
        // Hvordan: opprett controller med in-memory DbContext, legg til falsk OrganizationManager principal, assert redirect action/controller.
        [Fact]
        public async Task Index_OrganizationManagerUser_RedirectsToOrganizationManagerDashboard()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_OrgManager_Redirect")
                .Options;
            await using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "orgmanager-user"),
                new Claim(ClaimTypes.Role, "OrganizationManager")
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("OrganizationManager", redirect.ControllerName);
        }

        // Denne testen dekker den kritiske grenen: rollebasert redirect for piloter.
        // Hva: sikrer at Pilot-brukere sendes til sitt dashboard fra Home/Index.
        // Hvorfor: en regresjon her bryter navigasjonen for en hel rolle.
        // Hvordan: opprett controller med in-memory DbContext, legg til falsk Pilot principal, assert redirect action/controller.
        [Fact]
        public async Task Index_PilotUser_RedirectsToPilotDashboard()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_Pilot_Redirect")
                .Options;
            await using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "pilot-user"),
                new Claim(ClaimTypes.Role, "Pilot")
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Pilot", redirect.ControllerName);
        }

        // Denne testen dekker den kritiske grenen: redirect for brukere uten privilegerte roller.
        // Hva: sikrer at brukere som ikke er Admin eller Registrar sendes til Obstacle/DataForm.
        // Hvorfor: dette er standard navigasjon for vanlige brukere.
        // Hvordan: opprett controller med in-memory DbContext, legg til principal uten privilegerte roller, assert redirect til Obstacle/DataForm.
        [Fact]
        public async Task Index_UserWithoutPrivilegedRoles_RedirectsToObstacleForm()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_RegularUser_Redirect")
                .Options;
            await using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "regular-user")
                // Ingen privilegerte roller
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("DataForm", redirect.ActionName);
            Assert.Equal("Obstacle", redirect.ControllerName);
        }

        // Denne testen dekker den kritiske grenen: Admin-brukere får visning med databaseforbindelsestest.
        // Hva: sikrer at Admin-brukere får visning med suksessmelding når databaseforbindelsen fungerer.
        // Hvorfor: Admin-brukere må kunne se systemstatus.
        // Hvordan: opprett controller med in-memory DbContext (som alltid kan koble til), legg til Admin principal, assert at View returneres med suksessmelding.
        [Fact]
        public async Task Index_AdminUser_WithDatabaseConnection_ReturnsViewWithSuccessMessage()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_Admin_Success")
                .Options;
            await using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-user"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsType<string>(viewResult.Model);
            Assert.Contains("Connected to MariaDB successfully", model);
        }

        // Denne testen dekker den kritiske grenen: Privacy returnerer visning.
        // Hva: sikrer at Privacy-metoden returnerer en visning.
        // Hvorfor: dette er en enkel GET-metode som må fungere korrekt.
        // Hvordan: opprett controller, kall Privacy, assert at ViewResult returneres.
        [Fact]
        public void Privacy_ReturnsView()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_Privacy")
                .Options;
            using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);

            // Act
            var result = controller.Privacy();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view name
        }

        // Denne testen dekker den kritiske grenen: Error returnerer visning med ErrorViewModel.
        // Hva: sikrer at Error-metoden returnerer en visning med ErrorViewModel som inneholder RequestId.
        // Hvorfor: feilhåndtering må fungere korrekt for debugging.
        // Hvordan: opprett controller med HttpContext med TraceIdentifier, kall Error, assert at ErrorViewModel returneres med riktig RequestId.
        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Home_Error")
                .Options;
            using var db = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<HomeController>();
            var controller = new HomeController(db, logger);
            
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id";
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
            Assert.Equal("test-trace-id", model.RequestId);
        }
    }
}