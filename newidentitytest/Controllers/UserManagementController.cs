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
    /// Controller for managing users, including assigning them to organizations and roles.
    /// Requires Admin role to access.
    /// </summary>
    [Authorize(Roles = "Admin")] // Only admins can manage users
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: List all users
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

        // GET: View user details
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

        // POST: Assign user to organization
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

        // POST: Remove user from organization
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

        // Helper class for user view model
        public class UserViewModel
        {
            public ApplicationUser User { get; set; } = null!;
            public List<string> Roles { get; set; } = new();
        }
    }
}

