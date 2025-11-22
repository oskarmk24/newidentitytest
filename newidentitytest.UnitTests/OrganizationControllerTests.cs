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
    public class OrganizationControllerTests
    {
        // This test covers the critical access-control branch on reports.
        // What: ensure non-members without privileged roles get Forbid when requesting another org's reports.
        // Why: prevents leaking report data across organizations if role checks regress.
        // How: seed org + user in different org, attach fake principal, call Reports, expect ForbidResult.
        [Fact]
        public async Task Reports_UserNotInOrgAndNoRole_ReturnsForbid()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_Forbid")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Org A" });
            db.Organizations.Add(new Organization { Id = 2, Name = "Org B" });
            db.Users.Add(new ApplicationUser { Id = "user-1", OrganizationId = 2, Email = "user@b.com" });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-1")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Reports(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}
