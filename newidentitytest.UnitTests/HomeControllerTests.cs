using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using newidentitytest.Controllers;
using newidentitytest.Data;
using Xunit;

namespace newidentitytest.Tests
{
    public class HomeControllerTests
    {
        // This test targets a critical branch: role-based redirect for registrars.
        // What: ensure Registrar users are sent to their dashboard from Home/Index.
        // Why: a regression here breaks navigation for an entire role.
        // How: create controller with in-memory DbContext, attach a fake Registrar principal, assert redirect action/controller.
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
    }
}
