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
    public class OrganizationControllerTests
    {
        // Denne testen dekker den kritiske tilgangskontroll-grenen for rapporter.
        // Hva: sikrer at brukere som ikke er medlemmer og ikke har privilegerte roller får Forbid når de ber om andres organisasjonsrapporter.
        // Hvorfor: forhindrer at rapportdata lekkes mellom organisasjoner hvis rolle-sjekker feiler.
        // Hvordan: seed organisasjon + bruker i annen organisasjon, legg til falsk principal, kall Reports, forvent ForbidResult.
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

        // Denne testen dekker den kritiske grenen: Index returnerer liste over organisasjoner sortert alfabetisk.
        // Hva: sikrer at Index returnerer alle organisasjoner sortert etter navn.
        // Hvorfor: dette er hovedvisningen for organisasjoner.
        // Hvordan: seed flere organisasjoner, kall Index, assert at organisasjoner returneres sortert.
        [Fact]
        public async Task Index_ReturnsOrganizationsSortedByName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Index_Sorted")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Name = "Zebra Org" });
            db.Organizations.Add(new Organization { Name = "Alpha Org" });
            db.Organizations.Add(new Organization { Name = "Beta Org" });
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
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var organizations = Assert.IsAssignableFrom<List<Organization>>(viewResult.Model);
            Assert.Equal(3, organizations.Count);
            Assert.Equal("Alpha Org", organizations[0].Name);
            Assert.Equal("Beta Org", organizations[1].Name);
            Assert.Equal("Zebra Org", organizations[2].Name);
        }

        // Denne testen dekker den kritiske grenen: Details returnerer NotFound når id er null.
        // Hva: sikrer at Details håndterer null-id korrekt.
        // Hvorfor: forhindrer NullReferenceException.
        // Hvordan: kall Details med null, forvent NotFoundResult.
        [Fact]
        public async Task Details_NullId_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Details_Null")
                .Options;

            await using var db = new ApplicationDbContext(options);
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
            var result = await controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Details returnerer NotFound når organisasjon ikke finnes.
        // Hva: sikrer at Details håndterer manglende organisasjoner korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: kall Details med ikke-eksisterende ID, forvent NotFoundResult.
        [Fact]
        public async Task Details_NonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Details_NotFound")
                .Options;

            await using var db = new ApplicationDbContext(options);
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
            var result = await controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Details returnerer organisasjon med brukere.
        // Hva: sikrer at Details returnerer organisasjonen med tilknyttede brukere.
        // Hvorfor: dette er hovedfunksjonaliteten for detaljvisning.
        // Hvordan: seed organisasjon med brukere, kall Details, assert at organisasjon og brukere returneres.
        [Fact]
        public async Task Details_ValidId_ReturnsOrganizationWithUsers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Details_Success")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization 
            { 
                Id = 1, 
                Name = "Test Org",
                Description = "Test Description"
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user1@test.com",
                OrganizationId = 1 
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-2", 
                Email = "user2@test.com",
                OrganizationId = 1 
            });
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
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var organization = Assert.IsType<Organization>(viewResult.Model);
            Assert.Equal(1, organization.Id);
            Assert.Equal("Test Org", organization.Name);
            Assert.Equal(2, organization.Users.Count);
        }

        // Denne testen dekker den kritiske grenen: Create POST oppretter ny organisasjon når modellen er gyldig.
        // Hva: sikrer at Create POST oppretter organisasjon og redirecter til Index.
        // Hvorfor: dette er hovedfunksjonaliteten for organisasjonsadministrasjon.
        // Hvordan: opprett gyldig Organization, kall Create POST, assert at organisasjon opprettes og redirecter.
        [Fact]
        public async Task Create_ValidModel_CreatesOrganizationAndRedirects()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Create_Success")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            var organization = new Organization
            {
                Name = "New Organization",
                Description = "New Description"
            };

            // Act
            var result = await controller.Create(organization);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            var createdOrg = await db.Organizations.FirstOrDefaultAsync(o => o.Name == "New Organization");
            Assert.NotNull(createdOrg);
            Assert.Equal("New Organization", createdOrg.Name);
            Assert.Equal("New Description", createdOrg.Description);
        }

        // Denne testen dekker den kritiske grenen: Create POST returnerer View med feil når modellen er ugyldig.
        // Hva: sikrer at Create POST validerer modellen og returnerer View ved valideringsfeil.
        // Hvorfor: forhindrer opprettelse av ugyldige organisasjoner.
        // Hvordan: opprett ugyldig Organization (tom Name), kall Create POST, assert at View returneres.
        [Fact]
        public async Task Create_InvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Create_Invalid")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            var organization = new Organization
            {
                Name = "", // Ugyldig - tom streng
                Description = "Description"
            };
            controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await controller.Create(organization);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            
            // Sjekk at organisasjonen ikke ble opprettet
            var count = await db.Organizations.CountAsync();
            Assert.Equal(0, count);
        }

        // Denne testen dekker den kritiske grenen: Edit GET returnerer NotFound når id er null.
        // Hva: sikrer at Edit GET håndterer null-id korrekt.
        // Hvorfor: forhindrer NullReferenceException.
        // Hvordan: kall Edit GET med null, forvent NotFoundResult.
        [Fact]
        public async Task Edit_Get_NullId_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Edit_Get_Null")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Edit(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Edit GET returnerer NotFound når organisasjon ikke finnes.
        // Hva: sikrer at Edit GET håndterer manglende organisasjoner korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: kall Edit GET med ikke-eksisterende ID, forvent NotFoundResult.
        [Fact]
        public async Task Edit_Get_NonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Edit_Get_NotFound")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Edit(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Edit GET returnerer organisasjon for redigering.
        // Hva: sikrer at Edit GET returnerer organisasjonen for visning i redigeringsskjema.
        // Hvorfor: dette er hovedfunksjonaliteten for redigering.
        // Hvordan: seed organisasjon, kall Edit GET, assert at organisasjon returneres.
        [Fact]
        public async Task Edit_Get_ValidId_ReturnsOrganization()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Edit_Get_Success")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization 
            { 
                Id = 1, 
                Name = "Test Org",
                Description = "Test Description"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var organization = Assert.IsType<Organization>(viewResult.Model);
            Assert.Equal(1, organization.Id);
            Assert.Equal("Test Org", organization.Name);
        }

        // Denne testen dekker den kritiske grenen: Edit POST returnerer NotFound når id ikke matcher.
        // Hva: sikrer at Edit POST validerer at id matcher organisasjonens id.
        // Hvorfor: forhindrer feil ved redigering av feil organisasjon.
        // Hvordan: kall Edit POST med id som ikke matcher organisasjonens id, forvent NotFoundResult.
        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Edit_Post_IdMismatch")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            var organization = new Organization
            {
                Id = 1,
                Name = "Test Org"
            };

            // Act - kall med id=2, men organisasjon har id=1
            var result = await controller.Edit(2, organization);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Edit POST oppdaterer organisasjon når modellen er gyldig.
        // Hva: sikrer at Edit POST oppdaterer organisasjonen og redirecter til Index.
        // Hvorfor: dette er hovedfunksjonaliteten for organisasjonsredigering.
        // Hvordan: seed organisasjon, oppdater med ny data, kall Edit POST, assert at organisasjon oppdateres og redirecter.
        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesOrganizationAndRedirects()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Edit_Post_Success")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var existingOrg = new Organization 
            { 
                Id = 1, 
                Name = "Old Name",
                Description = "Old Description",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            db.Organizations.Add(existingOrg);
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            // Bruk den tracked instansen i stedet for å opprette ny
            existingOrg.Name = "New Name";
            existingOrg.Description = "New Description";

            // Act
            var result = await controller.Edit(1, existingOrg);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            var updatedOrg = await db.Organizations.FindAsync(1);
            Assert.NotNull(updatedOrg);
            Assert.Equal("New Name", updatedOrg.Name);
            Assert.Equal("New Description", updatedOrg.Description);
        }

        // Denne testen dekker den kritiske grenen: Edit POST returnerer View med feil når modellen er ugyldig.
        // Hva: sikrer at Edit POST validerer modellen og returnerer View ved valideringsfeil.
        // Hvorfor: forhindrer oppdatering med ugyldige data.
        // Hvordan: seed organisasjon, opprett ugyldig Organization, kall Edit POST, assert at View returneres.
        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Edit_Post_Invalid")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization 
            { 
                Id = 1, 
                Name = "Old Name",
                Description = "Old Description"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            var organization = new Organization
            {
                Id = 1,
                Name = "", // Ugyldig - tom streng
                Description = "Description"
            };
            controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await controller.Edit(1, organization);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            
            // Sjekk at organisasjonen ikke ble oppdatert
            var org = await db.Organizations.FindAsync(1);
            Assert.NotNull(org);
            Assert.Equal("Old Name", org.Name);
        }

        // Denne testen dekker den kritiske grenen: Delete GET returnerer NotFound når id er null.
        // Hva: sikrer at Delete GET håndterer null-id korrekt.
        // Hvorfor: forhindrer NullReferenceException.
        // Hvordan: kall Delete GET med null, forvent NotFoundResult.
        [Fact]
        public async Task Delete_Get_NullId_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Delete_Get_Null")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Delete GET returnerer NotFound når organisasjon ikke finnes.
        // Hva: sikrer at Delete GET håndterer manglende organisasjoner korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: kall Delete GET med ikke-eksisterende ID, forvent NotFoundResult.
        [Fact]
        public async Task Delete_Get_NonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Delete_Get_NotFound")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Delete GET returnerer organisasjon med brukere.
        // Hva: sikrer at Delete GET returnerer organisasjonen med tilknyttede brukere for visning.
        // Hvorfor: brukere må se konsekvenser av sletting.
        // Hvordan: seed organisasjon med brukere, kall Delete GET, assert at organisasjon og brukere returneres.
        [Fact]
        public async Task Delete_Get_ValidId_ReturnsOrganizationWithUsers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Delete_Get_Success")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization 
            { 
                Id = 1, 
                Name = "Test Org"
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user1@test.com",
                OrganizationId = 1 
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Delete(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var organization = Assert.IsType<Organization>(viewResult.Model);
            Assert.Equal(1, organization.Id);
            Assert.Equal(1, organization.Users.Count);
        }

        // Denne testen dekker den kritiske grenen: DeleteConfirmed sletter organisasjon.
        // Hva: sikrer at DeleteConfirmed sletter organisasjonen og redirecter til Index.
        // Hvorfor: dette er hovedfunksjonaliteten for organisasjonssletting.
        // Hvordan: seed organisasjon, kall DeleteConfirmed, assert at organisasjon slettes og redirecter.
        [Fact]
        public async Task DeleteConfirmed_ValidId_DeletesOrganizationAndRedirects()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_DeleteConfirmed_Success")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization 
            { 
                Id = 1, 
                Name = "Test Org"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            // Act
            var result = await controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            var deletedOrg = await db.Organizations.FindAsync(1);
            Assert.Null(deletedOrg);
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: Reports returnerer rapporter for Admin.
        // Hva: sikrer at Admin-brukere kan se alle organisasjonsrapporter.
        // Hvorfor: Admin-brukere har full tilgang til alle rapporter.
        // Hvordan: seed organisasjon med rapporter, kall Reports som Admin, assert at rapporter returneres.
        [Fact]
        public async Task Reports_AdminUser_ReturnsOrganizationReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_Admin")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Test Org" });
            // Legg til Admin-bruker i databasen
            db.Users.Add(new ApplicationUser 
            { 
                Id = "admin-1", 
                Email = "admin@test.com",
                OrganizationId = null // Admin trenger ikke organisasjon
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user@test.com",
                OrganizationId = 1 
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                ObstacleType = "Building"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Reports(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Single(reports);
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: Reports returnerer rapporter for Registrar.
        // Hva: sikrer at Registrar-brukere kan se alle organisasjonsrapporter.
        // Hvorfor: Registrar-brukere har full tilgang til alle rapporter.
        // Hvordan: seed organisasjon med rapporter, kall Reports som Registrar, assert at rapporter returneres.
        [Fact]
        public async Task Reports_RegistrarUser_ReturnsOrganizationReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_Registrar")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Test Org" });
            // Legg til Registrar-bruker i databasen
            db.Users.Add(new ApplicationUser 
            { 
                Id = "registrar-1", 
                Email = "registrar@test.com",
                OrganizationId = null // Registrar trenger ikke organisasjon
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user@test.com",
                OrganizationId = 1 
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "registrar-1"),
                        new Claim(ClaimTypes.Role, "Registrar")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Reports(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Single(reports);
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: Reports returnerer rapporter for OrganizationManager fra egen organisasjon.
        // Hva: sikrer at OrganizationManager kan se rapporter fra sin egen organisasjon.
        // Hvorfor: OrganizationManager skal kunne se rapporter fra sin organisasjon.
        // Hvordan: seed organisasjon med rapporter, kall Reports som OrganizationManager fra samme organisasjon, assert at rapporter returneres.
        [Fact]
        public async Task Reports_OrganizationManagerFromSameOrg_ReturnsOrganizationReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_OrgManager")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Test Org" });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "manager-1", 
                Email = "manager@test.com",
                OrganizationId = 1 
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user@test.com",
                OrganizationId = 1 
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
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
            var result = await controller.Reports(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(1, reports.Count);
        }

        // Denne testen dekker den kritiske tilgangskontroll-grenen: Reports returnerer Forbid for OrganizationManager fra annen organisasjon.
        // Hva: sikrer at OrganizationManager ikke kan se rapporter fra andre organisasjoner.
        // Hvorfor: forhindrer at organisasjonsledere ser andres organisasjonsrapporter.
        // Hvordan: seed to organisasjoner, kall Reports som OrganizationManager fra annen organisasjon, forvent ForbidResult.
        [Fact]
        public async Task Reports_OrganizationManagerFromDifferentOrg_ReturnsForbid()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_OrgManager_Forbid")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Org A" });
            db.Organizations.Add(new Organization { Id = 2, Name = "Org B" });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "manager-1", 
                Email = "manager@test.com",
                OrganizationId = 2 // Manager fra Org B
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
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

            // Act - prøv å se rapporter fra Org A
            var result = await controller.Reports(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Reports returnerer NotFound når id er null.
        // Hva: sikrer at Reports håndterer null-id korrekt.
        // Hvorfor: forhindrer NullReferenceException.
        // Hvordan: kall Reports med null, forvent NotFoundResult.
        [Fact]
        public async Task Reports_NullId_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_Null")
                .Options;

            await using var db = new ApplicationDbContext(options);
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
            var result = await controller.Reports(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Reports returnerer NotFound når organisasjon ikke finnes.
        // Hva: sikrer at Reports håndterer manglende organisasjoner korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: kall Reports med ikke-eksisterende ID, forvent NotFoundResult.
        [Fact]
        public async Task Reports_NonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_NotFound")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Reports(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Reports sorterer rapporter korrekt.
        // Hva: sikrer at Reports sorterer rapporter basert på sortBy- og sortOrder-parametrene.
        // Hvorfor: brukere trenger å kunne sortere rapporter for effektiv visning.
        // Hvordan: seed rapporter, kall Reports med sortBy="Id" og sortOrder="asc", assert at rapporter er sortert riktig.
        [Fact]
        public async Task Reports_SortsReportsCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_Sort")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Test Org" });
            // Legg til Admin-bruker i databasen
            db.Users.Add(new ApplicationUser 
            { 
                Id = "admin-1", 
                Email = "admin@test.com",
                OrganizationId = null
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user@test.com",
                OrganizationId = 1 
            });
            db.Reports.Add(new Report
            {
                Id = 3,
                UserId = "user-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                Id = 1,
                UserId = "user-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                Id = 2,
                UserId = "user-1",
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Reports(1, "Id", "asc");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(3, reports.Count);
            Assert.Equal(1, reports[0].Id);
            Assert.Equal(2, reports[1].Id);
            Assert.Equal(3, reports[2].Id);
        }

        // Denne testen dekker den kritiske grenen: Reports filtrerer rapporter basert på søk.
        // Hva: sikrer at Reports filtrerer rapporter basert på søkestreng i flere felt.
        // Hvorfor: brukere trenger å kunne søke etter spesifikke rapporter.
        // Hvordan: seed rapporter, kall Reports med search-parameter, assert at kun matchende rapporter returneres.
        [Fact]
        public async Task Reports_FiltersReportsBySearch()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Org_Reports_Search")
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.Organizations.Add(new Organization { Id = 1, Name = "Test Org" });
            // Legg til Admin-bruker i databasen
            db.Users.Add(new ApplicationUser 
            { 
                Id = "admin-1", 
                Email = "admin@test.com",
                OrganizationId = null
            });
            db.Users.Add(new ApplicationUser 
            { 
                Id = "user-1", 
                Email = "user@test.com",
                OrganizationId = 1 
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Pending",
                ObstacleType = "Building",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Pending",
                ObstacleType = "Tower",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                UserId = "user-1",
                Status = "Pending",
                ObstacleType = "Building",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new OrganizationController(db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = await controller.Reports(1, "CreatedAt", "desc", "Building");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(2, reports.Count);
            Assert.All(reports, r => Assert.Equal("Building", r.ObstacleType));
        }
    }
}
