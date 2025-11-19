using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for managing roles.
    /// Requires Admin or Registrar role to access.
    /// </summary>
    [Authorize(Roles = "Admin,Registrar")] // Admins and Registrars can manage roles
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // GET: List all roles
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
            return View(roles);
        }

        // GET: Create role form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create role
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

        // GET: View role details with users
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

        // GET: Manage user roles
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

        // POST: Assign role to user
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

        // POST: Remove role from user
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

        // GET: Delete role confirmation
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

        // POST: Delete role
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

