using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using newidentitytest.Controllers;
using newidentitytest.Data;
using newidentitytest.Models;
using Xunit;

namespace newidentitytest.Tests
{
    public class UserManagementControllerTests
    {
        // Denne testen dekker den kritiske grenen: Details returnerer NotFound når id er null.
        // Hva: sikrer at Details håndterer null-id korrekt.
        // Hvorfor: forhindrer NullReferenceException.
        // Hvordan: kall Details med null, forvent NotFoundResult.
        [Fact]
        public async Task Details_NullId_ReturnsNotFound()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserMgmt_Details_Null")
                .Options;
            await using var db = new ApplicationDbContext(options);

            var controller = new UserManagementController(userManagerMock.Object, roleManagerMock.Object, db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        

        // Denne testen dekker den kritiske grenen: AssignToOrganization tildeler bruker til organisasjon.
        // Hva: sikrer at AssignToOrganization oppdaterer brukerens OrganizationId og redirecter.
        // Hvorfor: dette er hovedfunksjonaliteten for organisasjons-tildeling.
        // Hvordan: seed bruker og organisasjon, mock UserManager, kall AssignToOrganization, assert at OrganizationId oppdateres.
        [Fact]
        public async Task AssignToOrganization_ValidUserAndOrg_AssignsAndRedirects()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserMgmt_Assign_Org")
                .Options;
            await using var db = new ApplicationDbContext(options);

            // Opprett organisasjon
            db.Organizations.Add(new Organization
            {
                Id = 1,
                Name = "Test Organization"
            });
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "user@test.com",
                UserName = "user@test.com"
            };

            // Mock FindByIdAsync
            userManagerMock.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Mock UpdateAsync til å returnere suksess
            userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new UserManagementController(userManagerMock.Object, roleManagerMock.Object, db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.AssignToOrganization("user-1", 1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            
            // Verifiser at UpdateAsync ble kalt med riktig OrganizationId
            userManagerMock.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => 
                u.Id == "user-1" && u.OrganizationId == 1)), Times.Once);
        }

        // Denne testen dekker den kritiske grenen: AssignToOrganization returnerer NotFound når bruker ikke finnes.
        // Hva: sikrer at AssignToOrganization validerer at bruker eksisterer.
        // Hvorfor: forhindrer feil ved tildeling til ikke-eksisterende brukere.
        // Hvordan: mock FindByIdAsync til å returnere null, kall AssignToOrganization, forvent NotFoundResult.
        [Fact]
        public async Task AssignToOrganization_NonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserMgmt_Assign_NoUser")
                .Options;
            await using var db = new ApplicationDbContext(options);

            // Mock FindByIdAsync til å returnere null
            userManagerMock.Setup(m => m.FindByIdAsync("non-existent-id"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = new UserManagementController(userManagerMock.Object, roleManagerMock.Object, db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.AssignToOrganization("non-existent-id", 1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: AssignToOrganization returnerer NotFound når organisasjon ikke finnes.
        // Hva: sikrer at AssignToOrganization validerer at organisasjon eksisterer.
        // Hvorfor: forhindrer feil ved tildeling til ikke-eksisterende organisasjoner.
        // Hvordan: seed bruker, kall AssignToOrganization med ikke-eksisterende orgId, forvent NotFoundResult.
        [Fact]
        public async Task AssignToOrganization_NonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserMgmt_Assign_NoOrg")
                .Options;
            await using var db = new ApplicationDbContext(options);

            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "user@test.com",
                UserName = "user@test.com"
            };

            userManagerMock.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            var controller = new UserManagementController(userManagerMock.Object, roleManagerMock.Object, db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.AssignToOrganization("user-1", 999); // Organisasjon som ikke eksisterer

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: RemoveFromOrganization fjerner bruker fra organisasjon.
        // Hva: sikrer at RemoveFromOrganization setter OrganizationId til null og redirecter.
        // Hvorfor: dette er hovedfunksjonaliteten for organisasjons-fjerning.
        // Hvordan: seed bruker med organisasjon, mock UserManager, kall RemoveFromOrganization, assert at OrganizationId settes til null.
        [Fact]
        public async Task RemoveFromOrganization_ValidUser_RemovesAndRedirects()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserMgmt_Remove_Org")
                .Options;
            await using var db = new ApplicationDbContext(options);

            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "user@test.com",
                UserName = "user@test.com",
                OrganizationId = 1 // Har organisasjon
            };

            userManagerMock.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new UserManagementController(userManagerMock.Object, roleManagerMock.Object, db);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.RemoveFromOrganization("user-1");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            
            // Verifiser at UpdateAsync ble kalt med OrganizationId = null
            userManagerMock.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => 
                u.Id == "user-1" && u.OrganizationId == null)), Times.Once);
        }

        // Denne testen dekker den kritiske grenen: RemoveFromOrganization returnerer NotFound når bruker ikke finnes.
        // Hva: sikrer at RemoveFromOrganization validerer at bruker eksisterer.
        // Hvorfor: forhindrer feil ved fjerning fra ikke-eksisterende brukere.
        // Hvordan: mock FindByIdAsync til å returnere null, kall RemoveFromOrganization, forvent NotFoundResult.
        [Fact]
        public async Task RemoveFromOrganization_NonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserMgmt_Remove_NoUser")
                .Options;
            await using var db = new ApplicationDbContext(options);

            userManagerMock.Setup(m => m.FindByIdAsync("non-existent-id"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = new UserManagementController(userManagerMock.Object, roleManagerMock.Object, db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.RemoveFromOrganization("non-existent-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
