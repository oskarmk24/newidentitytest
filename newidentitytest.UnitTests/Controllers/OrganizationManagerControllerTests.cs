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
    public class OrganizationManagerControllerTests
    {
        // Denne testen dekker den kritiske grenen: Index returnerer Forbid når userId mangler.
        // Hva: sikrer at Index returnerer Forbid hvis brukeren ikke har gyldig userId.
        // Hvorfor: forhindrer feil ved manglende brukerinformasjon.
        // Hvordan: kall Index uten userId claim, forvent ForbidResult.
        [Fact]
        public async Task Index_NoUserId_ReturnsForbid()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("OrgManager_Index_NoUserId")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationManagerController(db);
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

        // Denne testen dekker den kritiske grenen: Index returnerer NotFound når bruker ikke tilhører organisasjon.
        // Hva: sikrer at Index returnerer NotFound hvis brukeren ikke har organisasjon.
        // Hvorfor: organisasjonsledere må tilhøre en organisasjon.
        // Hvordan: seed bruker uten OrganizationId, kall Index, forvent NotFoundObjectResult.
        [Fact]
        public async Task Index_UserWithoutOrganization_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("OrgManager_Index_NoOrg")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett bruker uten organisasjon
            db.Users.Add(new ApplicationUser
            {
                Id = "manager-1",
                Email = "manager@test.com",
                UserName = "manager@test.com",
                OrganizationId = null // Ingen organisasjon
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationManagerController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "manager-1"),
                        new Claim(ClaimTypes.Role, "OrganizationManager")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User organization not found", notFoundResult.Value);
        }

        // Denne testen dekker den kritiske grenen: Index beregner korrekt statistikk for organisasjonens rapporter.
        // Hva: sikrer at Index beregner og viser korrekt antall rapporter med forskjellige statuser.
        // Hvorfor: dashboardet må vise korrekt informasjon for organisasjonslederen.
        // Hvordan: seed organisasjon med medlemmer og rapporter, kall Index, assert at ViewBag-verdiene er korrekte.
        [Fact]
        public async Task Index_CalculatesCorrectStatistics()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("OrgManager_Index_Statistics")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett organisasjon
            var organization = new Organization
            {
                Id = 1,
                Name = "Test Organization"
            };
            db.Organizations.Add(organization);
            
            // Opprett organisasjonsleder
            var manager = new ApplicationUser
            {
                Id = "manager-1",
                Email = "manager@test.com",
                UserName = "manager@test.com",
                OrganizationId = 1
            };
            db.Users.Add(manager);
            
            // Opprett andre medlemmer i samme organisasjon
            var member1 = new ApplicationUser
            {
                Id = "member-1",
                Email = "member1@test.com",
                UserName = "member1@test.com",
                OrganizationId = 1
            };
            var member2 = new ApplicationUser
            {
                Id = "member-2",
                Email = "member2@test.com",
                UserName = "member2@test.com",
                OrganizationId = 1
            };
            db.Users.Add(member1);
            db.Users.Add(member2);
            
            // Opprett rapporter fra organisasjonens medlemmer
            db.Reports.Add(new Report
            {
                UserId = "member-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "member-1",
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "member-2",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "member-2",
                Status = "Rejected",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[13.0,62.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationManagerController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "manager-1"),
                        new Claim(ClaimTypes.Role, "OrganizationManager")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(4, viewResult.ViewData["TotalReports"]); // Totalt antall
            Assert.Equal(2, viewResult.ViewData["PendingReports"]); // Kun Pending
            Assert.Equal(1, viewResult.ViewData["ApprovedReports"]); // Kun Approved
            Assert.Equal(1, viewResult.ViewData["RejectedReports"]); // Kun Rejected
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: Index viser kun rapporter fra organisasjonens medlemmer.
        // Hva: sikrer at Index kun viser rapporter fra brukere i samme organisasjon.
        // Hvorfor: forhindrer at organisasjonsledere ser rapporter fra andre organisasjoner.
        // Hvordan: seed to organisasjoner med rapporter, kall Index som manager fra org 1, assert at kun org 1s rapporter telles.
        [Fact]
        public async Task Index_OnlyShowsReportsFromOwnOrganization()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("OrgManager_Index_Isolation")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett to organisasjoner
            var org1 = new Organization { Id = 1, Name = "Organization 1" };
            var org2 = new Organization { Id = 2, Name = "Organization 2" };
            db.Organizations.Add(org1);
            db.Organizations.Add(org2);
            
            // Opprett organisasjonsleder for org 1
            var manager1 = new ApplicationUser
            {
                Id = "manager-1",
                Email = "manager1@test.com",
                UserName = "manager1@test.com",
                OrganizationId = 1
            };
            db.Users.Add(manager1);
            
            // Opprett medlemmer i hver organisasjon
            var member1 = new ApplicationUser
            {
                Id = "member-1",
                Email = "member1@test.com",
                UserName = "member1@test.com",
                OrganizationId = 1
            };
            var member2 = new ApplicationUser
            {
                Id = "member-2",
                Email = "member2@test.com",
                UserName = "member2@test.com",
                OrganizationId = 2
            };
            db.Users.Add(member1);
            db.Users.Add(member2);
            
            // Opprett rapporter fra begge organisasjoner
            db.Reports.Add(new Report
            {
                UserId = "member-1", // Fra org 1
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "member-1", // Fra org 1
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "member-2", // Fra org 2 - skal IKKE vises
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationManagerController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "manager-1"), // Manager fra org 1
                        new Claim(ClaimTypes.Role, "OrganizationManager")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(2, viewResult.ViewData["TotalReports"]); // Kun org 1s rapporter
            Assert.Equal(1, viewResult.ViewData["PendingReports"]);
            Assert.Equal(1, viewResult.ViewData["ApprovedReports"]);
            Assert.Equal(0, viewResult.ViewData["RejectedReports"]);
            
            // Verifiser at organisasjonen er riktig
            var org = viewResult.ViewData["Organization"] as Organization;
            Assert.NotNull(org);
            Assert.Equal(1, org.Id);
            Assert.Equal("Organization 1", org.Name);
        }

        // Denne testen dekker den kritiske grenen: Index returnerer NotFound når organisasjon ikke finnes.
        // Hva: sikrer at Index returnerer NotFound hvis brukerens organisasjon ikke finnes i databasen.
        // Hvorfor: systemet må håndtere edge cases hvor organisasjonen er slettet eller ikke finnes.
        // Hvordan: seed bruker med OrganizationId som ikke eksisterer, kall Index, forvent NotFoundObjectResult.
        [Fact]
        public async Task Index_OrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("OrgManager_Index_OrgNotFound")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett bruker med OrganizationId som ikke eksisterer
            db.Users.Add(new ApplicationUser
            {
                Id = "manager-1",
                Email = "manager@test.com",
                UserName = "manager@test.com",
                OrganizationId = 999 // Organisasjon som ikke eksisterer
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationManagerController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "manager-1"),
                        new Claim(ClaimTypes.Role, "OrganizationManager")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Organization not found", notFoundResult.Value);
        }
    }
}
