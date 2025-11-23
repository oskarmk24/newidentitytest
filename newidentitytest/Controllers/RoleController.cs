using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for administrasjon av roller.
    /// Støtter opprettelse, visning, sletting av roller og tildeling/fjerning av roller til brukere.
    /// Krever Admin eller Registrar rolle for alle operasjoner.
    /// </summary>
    [Authorize(Roles = "Admin,Registrar")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initialiserer controlleren med RoleManager og UserManager for rolle- og brukerhåndtering.
        /// </summary>
        public RoleController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Viser liste over alle roller i systemet.
        /// Sortert alfabetisk etter navn.
        /// </summary>
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
            return View(roles);
        }

        /// <summary>
        /// Viser skjema for å opprette ny rolle.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Oppretter en ny rolle basert på rolle-navn.
        /// Validerer at rolle-navnet er oppgitt og ikke allerede eksisterer.
        /// Viser feilmeldinger hvis validering feiler eller opprettelsen mislykkes.
        /// Ved suksess: redirecter til Index med suksessmelding.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Role name is required");
                return View();
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError("", "Role already exists");
                return View();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{roleName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View();
        }

        /// <summary>
        /// Viser detaljvisning av en spesifikk rolle inkludert alle brukere som har denne rollen.
        /// Returnerer NotFound hvis rolle-ID er null eller rollen ikke finnes.
        /// </summary>
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

            ViewBag.RoleName = role.Name;
            ViewBag.UsersInRole = usersInRole;

            return View(role);
        }

        /// <summary>
        /// Viser skjema for å administrere roller for en spesifikk bruker.
        /// Viser brukerens nåværende roller og alle tilgjengelige roller i systemet.
        /// Returnerer NotFound hvis brukeren ikke finnes.
        /// </summary>
        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.OrderBy(r => r.Name).ToList();

            ViewBag.UserId = userId;
            ViewBag.UserName = user.UserName;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = allRoles.Select(r => r.Name).ToList();

            return View();
        }

        /// <summary>
        /// Tildeler en rolle til en bruker.
        /// Sjekker at brukeren ikke allerede har rollen før tildeling.
        /// Redirecter tilbake til ManageUserRoles med suksessmelding eller feilmelding.
        /// Returnerer NotFound hvis brukeren ikke finnes.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Role '{roleName}' assigned to user successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign role.";
                }
            }

            return RedirectToAction(nameof(ManageUserRoles), new { userId });
        }

        /// <summary>
        /// Fjerner en rolle fra en bruker.
        /// Sjekker at brukeren har rollen før fjerning.
        /// Redirecter tilbake til ManageUserRoles med suksessmelding eller feilmelding.
        /// Returnerer NotFound hvis brukeren ikke finnes.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Role '{roleName}' removed from user successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to remove role.";
                }
            }

            return RedirectToAction(nameof(ManageUserRoles), new { userId });
        }

        /// <summary>
        /// Viser bekreftelsesside for sletting av rolle.
        /// Inkluderer liste over alle brukere som har denne rollen for å vise konsekvenser av sletting.
        /// Returnerer NotFound hvis rolle-ID er null eller rollen ikke finnes.
        /// </summary>
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            ViewBag.UsersInRole = usersInRole;

            return View(role);
        }

        /// <summary>
        /// Sletter en rolle permanent fra systemet.
        /// Sjekker at rollen ikke har brukere tildelt før sletting.
        /// Hvis rollen har brukere, vises feilmelding og redirecter til Index.
        /// Ved suksess: redirecter til Index med suksessmelding.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                if (usersInRole.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete role '{role.Name}' because it has users assigned. Remove users from the role first.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete role.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

