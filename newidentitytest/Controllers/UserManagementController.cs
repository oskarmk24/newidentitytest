using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for administrasjon av brukere.
    /// Støtter visning av alle brukere med roller, detaljvisning, og tildeling/fjerning av brukere til organisasjoner.
    /// Roller administreres via RoleController.
    /// Krever Admin eller Registrar rolle for alle operasjoner.
    /// </summary>
    [Authorize(Roles = "Admin,Registrar")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initialiserer controlleren med UserManager, RoleManager og ApplicationDbContext for bruker-, rolle- og databaseoperasjoner.
        /// </summary>
        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Viser liste over alle brukere i systemet med deres organisasjoner og roller.
        /// Sortert alfabetisk etter e-postadresse.
        /// Henter roller for hver bruker og pakker dem inn i UserViewModel for visning.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.Organization)
                .OrderBy(u => u.Email)
                .ToListAsync();

            var usersWithRoles = new List<UserViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            return View(usersWithRoles);
        }

        /// <summary>
        /// Viser detaljvisning av en spesifikk bruker.
        /// Inkluderer brukerens organisasjon, roller og alle tilgjengelige organisasjoner for tildeling.
        /// Returnerer NotFound hvis bruker-ID er null eller brukeren ikke finnes.
        /// </summary>
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = roles;
            ViewBag.AllOrganizations = await _context.Organizations.OrderBy(o => o.Name).ToListAsync();

            return View(user);
        }

        /// <summary>
        /// Tildeler en bruker til en organisasjon.
        /// Validerer at både brukeren og organisasjonen eksisterer.
        /// Oppdaterer brukerens OrganizationId og lagrer endringen.
        /// Redirecter tilbake til Details med suksessmelding eller feilmelding.
        /// Returnerer NotFound hvis brukeren eller organisasjonen ikke finnes.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToOrganization(string userId, int organizationId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                return NotFound();
            }

            user.OrganizationId = organizationId;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User assigned to '{organization.Name}' successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to assign user to organization.";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        /// <summary>
        /// Fjerner en bruker fra sin organisasjon ved å sette OrganizationId til null.
        /// Validerer at brukeren eksisterer.
        /// Oppdaterer brukeren og lagrer endringen.
        /// Redirecter tilbake til Details med suksessmelding eller feilmelding.
        /// Returnerer NotFound hvis brukeren ikke finnes.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromOrganization(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.OrganizationId = null;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User removed from organization successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove user from organization.";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        /// <summary>
        /// ViewModel-klasse for å pakke brukerinformasjon sammen med brukerens roller.
        /// Brukes i Index-visningen for å vise alle brukere med deres tilknyttede roller.
        /// </summary>
        public class UserViewModel
        {
            /// <summary>
            /// Brukeren som skal vises.
            /// </summary>
            public ApplicationUser User { get; set; } = null!;
            
            /// <summary>
            /// Liste over alle roller som brukeren har.
            /// </summary>
            public List<string> Roles { get; set; } = new();
        }
    }
}

