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
    public class RegistrarControllerTests
    {
        // Denne testen dekker den kritiske grenen: Index beregner korrekt statistikk for rapporter.
        // Hva: sikrer at Index beregner og viser korrekt antall totale rapporter og ventende rapporter.
        // Hvorfor: dashboardet må vise korrekt informasjon for registerføreren.
        // Hvordan: seed rapporter med forskjellige statuser, kall Index, assert at ViewBag-verdiene er korrekte.
        [Fact]
        public async Task Index_CalculatesCorrectStatistics()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Registrar_Index_Statistics")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter med forskjellige statuser
            db.Reports.Add(new Report
            {
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                Status = "Approved",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            db.Reports.Add(new Report
            {
                Status = "Rejected",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[13.0,62.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new RegistrarController(db);
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
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(4, viewResult.ViewData["ReportCount"]); // Totalt antall
            Assert.Equal(2, viewResult.ViewData["PendingReportsCount"]); // Kun Pending
        }

        // Denne testen dekker den kritiske grenen: Index viser kun tildelte saker for den innloggede registerføreren på dashboardet.
        // Hva: sikrer at Index kun viser rapporter som er tildelt den autentiserte registerføreren i "My Assigned Reports"-seksjonen.
        // Hvorfor: dashboardet skal vise en oversikt over registerførerens egne tildelte saker (widget).
        // Hvorfor: dette er kun en widget på dashboardet - registerførere kan fortsatt se alle rapporter via Pending().
        // Hvordan: seed rapporter tildelt forskjellige registerførere, kall Index, assert at kun riktige rapporter vises i widget.
        [Fact]
        public async Task Index_ShowsOnlyAssignedReportsInDashboardWidget()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Registrar_Index_Assigned")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter tildelt forskjellige registerførere
            db.Reports.Add(new Report
            {
                Status = "Pending",
                AssignedRegistrarId = "registrar-1",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                AssignedRegistrarId = "registrar-2",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                AssignedRegistrarId = "registrar-1",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}",
                CreatedAt = DateTime.UtcNow
            });
            db.Reports.Add(new Report
            {
                Status = "Approved", // Ikke Pending, skal ikke vises i widget
                AssignedRegistrarId = "registrar-1",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[13.0,62.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new RegistrarController(db);
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
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var assignedReports = viewResult.ViewData["MyAssignedReports"] as List<Report>;
            Assert.NotNull(assignedReports);
            Assert.Equal(2, assignedReports.Count); // Kun Pending-rapporter tildelt registrar-1
            Assert.All(assignedReports, r => 
            {
                Assert.Equal("registrar-1", r.AssignedRegistrarId);
                Assert.Equal("Pending", r.Status);
            });
        }

        // Denne testen dekker den kritiske grenen: Index håndterer manglende registrarId korrekt.
        // Hva: sikrer at Index fungerer korrekt når registrarId mangler (tom liste for tildelte saker).
        // Hvorfor: systemet må håndtere edge cases hvor brukerinformasjon mangler.
        // Hvordan: kall Index uten NameIdentifier claim, assert at MyAssignedReports er tom liste.
        [Fact]
        public async Task Index_NoRegistrarId_ReturnsEmptyAssignedReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Registrar_Index_NoId")
                .Options;

            await using var db = new ApplicationDbContext(options);
            var controller = new RegistrarController(db);
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
            var viewResult = Assert.IsType<ViewResult>(result);
            var assignedReports = viewResult.ViewData["MyAssignedReports"] as List<Report>;
            Assert.NotNull(assignedReports);
            Assert.Empty(assignedReports);
        }

        // Denne testen dekker den kritiske grenen: Pending returnerer ALLE rapporter med status "Pending".
        // Hva: sikrer at Pending viser alle ventende rapporter, ikke kun tildelte.
        // Hvorfor: registerførere skal kunne se alle ventende rapporter for å kunne tildele og behandle dem.
        // Hvordan: seed rapporter med forskjellige statuser og tildelinger, kall Pending, assert at alle Pending-rapporter returneres.
        [Fact]
        public async Task Pending_ReturnsAllPendingReports()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Registrar_Pending_All")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter - noen tildelt, noen ikke tildelt
            db.Reports.Add(new Report
            {
                Status = "Pending",
                AssignedRegistrarId = "registrar-1",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}",
                ObstacleType = "Building"
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                AssignedRegistrarId = null, // Ikke tildelt
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}",
                ObstacleType = "Tower"
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                AssignedRegistrarId = "registrar-2", // Tildelt annen registerfører
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}",
                ObstacleType = "Antenna"
            });
            db.Reports.Add(new Report
            {
                Status = "Approved", // Ikke Pending
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[13.0,62.0]}",
                ObstacleType = "Building"
            });
            await db.SaveChangesAsync();

            var controller = new RegistrarController(db);
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
            var result = await controller.Pending();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(3, reports.Count); // Alle Pending-rapporter, uavhengig av tildeling
            Assert.All(reports, r => Assert.Equal("Pending", r.Status));
        }

        // Denne testen dekker den kritiske grenen: Pending sorterer rapporter korrekt.
        // Hva: sikrer at Pending sorterer rapporter basert på sortBy- og sortOrder-parametrene.
        // Hvorfor: registerførere trenger å kunne sortere rapporter for effektiv behandling.
        // Hvordan: seed rapporter, kall Pending med sortBy="Id" og sortOrder="asc", assert at rapporter er sortert riktig.
        [Fact]
        public async Task Pending_SortsReportsCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Registrar_Pending_Sort")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter med forskjellige ID-er
            db.Reports.Add(new Report
            {
                Id = 3,
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                Id = 1,
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                Id = 2,
                Status = "Pending",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new RegistrarController(db);
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
            var result = await controller.Pending("Id", "asc");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(3, reports.Count);
            Assert.Equal(1, reports[0].Id);
            Assert.Equal(2, reports[1].Id);
            Assert.Equal(3, reports[2].Id);
        }

        // Denne testen dekker den kritiske grenen: Pending filtrerer rapporter basert på søk.
        // Hva: sikrer at Pending filtrerer rapporter basert på søkestreng i flere felt.
        // Hvorfor: registerførere trenger å kunne søke etter spesifikke rapporter.
        // Hvordan: seed rapporter, kall Pending med search-parameter, assert at kun matchende rapporter returneres.
        [Fact]
        public async Task Pending_FiltersReportsBySearch()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Registrar_Pending_Search")
                .Options;

            await using var db = new ApplicationDbContext(options);
            
            // Opprett rapporter med forskjellige typer
            db.Reports.Add(new Report
            {
                Status = "Pending",
                ObstacleType = "Building",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[10.0,59.0]}"
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                ObstacleType = "Tower",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[11.0,60.0]}"
            });
            db.Reports.Add(new Report
            {
                Status = "Pending",
                ObstacleType = "Building",
                ObstacleLocation = "{\"type\":\"Point\",\"coordinates\":[12.0,61.0]}"
            });
            await db.SaveChangesAsync();

            var controller = new RegistrarController(db);
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
            var result = await controller.Pending("CreatedAt", "desc", "Building");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var reports = Assert.IsAssignableFrom<List<ReportListItem>>(viewResult.Model);
            Assert.Equal(2, reports.Count);
            Assert.All(reports, r => Assert.Equal("Building", r.ObstacleType));
        }
    }
}
