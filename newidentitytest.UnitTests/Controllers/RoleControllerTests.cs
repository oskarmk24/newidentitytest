using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using newidentitytest.Controllers;
using newidentitytest.Models;
using Xunit;

namespace newidentitytest.Tests
{
    public class RoleControllerTests
    {
        // Denne testen dekker den kritiske grenen: Create oppretter ny rolle når rolle ikke eksisterer.
        // Hva: sikrer at Create oppretter en ny rolle og redirecter til Index med suksessmelding.
        // Hvorfor: dette er hovedfunksjonaliteten for rolleadministrasjon.
        // Hvordan: mock RoleManager og UserManager, kall Create med nytt rolle-navn, assert at rolle opprettes og redirecter.
        [Fact]
        public async Task Create_NewRole_CreatesRoleAndRedirects()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            // Mock RoleExistsAsync til å returnere false (rolle eksisterer ikke)
            roleManagerMock.Setup(m => m.RoleExistsAsync("NewRole"))
                .ReturnsAsync(false);

            // Mock CreateAsync til å returnere suksess
            var identityResult = IdentityResult.Success;
            roleManagerMock.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(identityResult);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.Create("NewRole");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            // Verifiser at CreateAsync ble kalt
            roleManagerMock.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == "NewRole")), Times.Once);
        }

        // Denne testen dekker den kritiske grenen: Create returnerer feilmelding når rolle allerede eksisterer.
        // Hva: sikrer at Create validerer at rolle ikke allerede eksisterer.
        // Hvorfor: forhindrer duplikate roller i systemet.
        // Hvordan: mock RoleExistsAsync til å returnere true, kall Create, assert at View returneres med feilmelding.
        [Fact]
        public async Task Create_ExistingRole_ReturnsViewWithError()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            // Mock RoleExistsAsync til å returnere true (rolle eksisterer allerede)
            roleManagerMock.Setup(m => m.RoleExistsAsync("ExistingRole"))
                .ReturnsAsync(true);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Create("ExistingRole");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("already exists", controller.ModelState[""].Errors[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
            
            // Verifiser at CreateAsync IKKE ble kalt
            roleManagerMock.Verify(m => m.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        // Denne testen dekker den kritiske grenen: Create validerer at rolle-navn er oppgitt.
        // Hva: sikrer at Create krever at rolle-navn er oppgitt.
        // Hvorfor: forhindrer opprettelse av roller uten navn.
        // Hvordan: kall Create med tom streng, assert at View returneres med feilmelding.
        [Fact]
        public async Task Create_EmptyRoleName_ReturnsViewWithError()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Create("");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("required", controller.ModelState[""].Errors[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // Denne testen dekker den kritiske grenen: Details returnerer NotFound når rolle ikke finnes.
        // Hva: sikrer at Details håndterer manglende roller korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: mock FindByIdAsync til å returnere null, kall Details, forvent NotFoundResult.
        [Fact]
        public async Task Details_NonExistentRole_ReturnsNotFound()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            // Mock FindByIdAsync til å returnere null
            roleManagerMock.Setup(m => m.FindByIdAsync("non-existent-id"))
                .ReturnsAsync((IdentityRole?)null);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Details("non-existent-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: Details returnerer NotFound når id er null.
        // Hva: sikrer at Details håndterer null-id korrekt.
        // Hvorfor: forhindrer NullReferenceException.
        // Hvordan: kall Details med null, forvent NotFoundResult.
        [Fact]
        public async Task Details_NullId_ReturnsNotFound()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Denne testen dekker den kritiske grenen: AssignRole tildeler rolle til bruker når bruker ikke allerede har rollen.
        // Hva: sikrer at AssignRole tildeler rolle til bruker og redirecter med suksessmelding.
        // Hvorfor: dette er hovedfunksjonaliteten for rolle-tildeling.
        // Hvordan: mock UserManager, kall AssignRole, assert at AddToRoleAsync kalles og redirecter.
        [Fact]
        public async Task AssignRole_UserNotInRole_AssignsRoleAndRedirects()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var user = new ApplicationUser { Id = "user-1", Email = "user@test.com" };

            // Mock FindByIdAsync
            userManagerMock.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Mock IsInRoleAsync til å returnere false (bruker har ikke rollen)
            userManagerMock.Setup(m => m.IsInRoleAsync(user, "Pilot"))
                .ReturnsAsync(false);

            // Mock AddToRoleAsync til å returnere suksess
            userManagerMock.Setup(m => m.AddToRoleAsync(user, "Pilot"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.AssignRole("user-1", "Pilot");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageUserRoles", redirect.ActionName);
            
            // Verifiser at AddToRoleAsync ble kalt
            userManagerMock.Verify(m => m.AddToRoleAsync(user, "Pilot"), Times.Once);
        }

        // Denne testen dekker den kritiske grenen: AssignRole gjør ingenting når bruker allerede har rollen.
        // Hva: sikrer at AssignRole ikke tildeler rolle hvis bruker allerede har den.
        // Hvorfor: forhindrer duplikate rolle-tildelinger.
        // Hvordan: mock IsInRoleAsync til å returnere true, kall AssignRole, assert at AddToRoleAsync ikke kalles.
        [Fact]
        public async Task AssignRole_UserAlreadyInRole_DoesNotAssignAgain()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var user = new ApplicationUser { Id = "user-1", Email = "user@test.com" };

            userManagerMock.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Mock IsInRoleAsync til å returnere true (bruker har allerede rollen)
            userManagerMock.Setup(m => m.IsInRoleAsync(user, "Pilot"))
                .ReturnsAsync(true);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.AssignRole("user-1", "Pilot");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            
            // Verifiser at AddToRoleAsync IKKE ble kalt
            userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        // Denne testen dekker den kritiske grenen: RemoveRole fjerner rolle fra bruker når bruker har rollen.
        // Hva: sikrer at RemoveRole fjerner rolle fra bruker og redirecter med suksessmelding.
        // Hvorfor: dette er hovedfunksjonaliteten for rolle-fjerning.
        // Hvordan: mock UserManager, kall RemoveRole, assert at RemoveFromRoleAsync kalles og redirecter.
        [Fact]
        public async Task RemoveRole_UserInRole_RemovesRoleAndRedirects()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var user = new ApplicationUser { Id = "user-1", Email = "user@test.com" };

            userManagerMock.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Mock IsInRoleAsync til å returnere true (bruker har rollen)
            userManagerMock.Setup(m => m.IsInRoleAsync(user, "Pilot"))
                .ReturnsAsync(true);

            // Mock RemoveFromRoleAsync til å returnere suksess
            userManagerMock.Setup(m => m.RemoveFromRoleAsync(user, "Pilot"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.RemoveRole("user-1", "Pilot");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageUserRoles", redirect.ActionName);
            
            // Verifiser at RemoveFromRoleAsync ble kalt
            userManagerMock.Verify(m => m.RemoveFromRoleAsync(user, "Pilot"), Times.Once);
        }

        // Denne testen dekker den kritiske grenen: DeleteConfirmed sletter rolle når ingen brukere har rollen.
        // Hva: sikrer at DeleteConfirmed sletter rolle når den er tom for brukere.
        // Hvorfor: dette er hovedfunksjonaliteten for rolle-sletting.
        // Hvordan: mock RoleManager og UserManager, kall DeleteConfirmed, assert at DeleteAsync kalles og redirecter.
        [Fact]
        public async Task DeleteConfirmed_RoleWithoutUsers_DeletesRoleAndRedirects()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var role = new IdentityRole { Id = "role-1", Name = "TestRole" };

            // Mock FindByIdAsync
            roleManagerMock.Setup(m => m.FindByIdAsync("role-1"))
                .ReturnsAsync(role);

            // Mock GetUsersInRoleAsync til å returnere tom liste (ingen brukere har rollen)
            userManagerMock.Setup(m => m.GetUsersInRoleAsync("TestRole"))
                .ReturnsAsync(new List<ApplicationUser>());

            // Mock DeleteAsync til å returnere suksess
            roleManagerMock.Setup(m => m.DeleteAsync(role))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.DeleteConfirmed("role-1");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            // Verifiser at DeleteAsync ble kalt
            roleManagerMock.Verify(m => m.DeleteAsync(role), Times.Once);
        }

        // Denne testen dekker den kritiske grenen: DeleteConfirmed forhindrer sletting når rolle har brukere.
        // Hva: sikrer at DeleteConfirmed ikke sletter rolle hvis den har brukere tildelt.
        // Hvorfor: forhindrer sletting av roller som er i bruk.
        // Hvordan: mock GetUsersInRoleAsync til å returnere brukere, kall DeleteConfirmed, assert at feilmelding vises og DeleteAsync ikke kalles.
        [Fact]
        public async Task DeleteConfirmed_RoleWithUsers_PreventsDeletion()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var role = new IdentityRole { Id = "role-1", Name = "Pilot" };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "user-1", Email = "user1@test.com" }
            };

            roleManagerMock.Setup(m => m.FindByIdAsync("role-1"))
                .ReturnsAsync(role);

            // Mock GetUsersInRoleAsync til å returnere brukere (rollen har brukere)
            userManagerMock.Setup(m => m.GetUsersInRoleAsync("Pilot"))
                .ReturnsAsync(users);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(httpContext, new MockTempDataProvider());
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.DeleteConfirmed("role-1");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            
            // Verifiser at DeleteAsync IKKE ble kalt
            roleManagerMock.Verify(m => m.DeleteAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        // Denne testen dekker den kritiske grenen: ManageUserRoles returnerer NotFound når bruker ikke finnes.
        // Hva: sikrer at ManageUserRoles håndterer manglende brukere korrekt.
        // Hvorfor: systemet må håndtere edge cases hvor data ikke finnes.
        // Hvordan: mock FindByIdAsync til å returnere null, kall ManageUserRoles, forvent NotFoundResult.
        [Fact]
        public async Task ManageUserRoles_NonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);
            
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            // Mock FindByIdAsync til å returnere null
            userManagerMock.Setup(m => m.FindByIdAsync("non-existent-id"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = new RoleController(roleManagerMock.Object, userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.ManageUserRoles("non-existent-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
