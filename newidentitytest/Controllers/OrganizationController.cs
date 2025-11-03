using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for managing organizations.
    /// Requires authentication to access.
    /// </summary>
    [Authorize] // Require authentication
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: List all organizations
        public async Task<IActionResult> Index()
        {
            var organizations = await _context.Organizations
                .Include(o => o.Users)
                .OrderBy(o => o.Name)
                .ToListAsync();
            return View(organizations);
        }

        // GET: View organization details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        // GET: Create organization form
        [Authorize(Roles = "Admin,Manager")] // Only Admins and Managers can create organizations
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create organization
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([Bind("Name,Description")] Organization organization)
        {
            if (ModelState.IsValid)
            {
                _context.Add(organization);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Organization '{organization.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(organization);
        }

        // GET: Edit organization
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                return NotFound();
            }
            return View(organization);
        }

        // POST: Update organization
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CreatedAt")] Organization organization)
        {
            if (id != organization.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(organization);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Organization '{organization.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizationExists(organization.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(organization);
        }

        // GET: Delete organization
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        // POST: Delete organization
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var organization = await _context.Organizations.FindAsync(id);
            if (organization != null)
            {
                _context.Organizations.Remove(organization);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Organization '{organization.Name}' deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrganizationExists(int id)
        {
            return _context.Organizations.Any(e => e.Id == id);
        }
    }
}

