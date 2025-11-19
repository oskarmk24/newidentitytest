using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
        [Authorize(Roles = "Admin,Registrar,Manager")] // Admins, Registrars and Managers can create organizations
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create organization
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Registrar,Manager")]
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
        [Authorize(Roles = "Admin,Registrar,Manager")]
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
        [Authorize(Roles = "Admin,Registrar,Manager")]
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
        [Authorize(Roles = "Admin,Registrar")]
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
        [Authorize(Roles = "Admin,Registrar")]
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

        // GET: View reports from organization members
        [HttpGet]
        public async Task<IActionResult> Reports(int? id, string sortBy = "CreatedAt", string sortOrder = "desc", string search = "")
        {
            if (id == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (organization == null)
            {
                return NotFound();
            }

            // Check if user has access: must be member of organization, Admin, Registrar, or OrganizationManager
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            bool hasAccess = false;
            if (user != null)
            {
                // Admin and Registrar can see all organization reports
                if (User.IsInRole("Admin") || User.IsInRole("Registrar"))
                {
                    hasAccess = true;
                }
                // OrganizationManager can only see their own organization's reports
                else if (User.IsInRole("OrganizationManager") && user.OrganizationId == id)
                {
                    hasAccess = true;
                }
                // Users can see reports from their own organization
                else if (user.OrganizationId == id)
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                return Forbid();
            }

            // Get user IDs for this organization
            var organizationUserIds = await _context.Users
                .Where(u => u.OrganizationId == id)
                .Select(u => u.Id)
                .ToListAsync();

            // Build query for reports from organization members
            var query = from r in _context.Reports
                        where organizationUserIds.Contains(r.UserId)
                        join u in _context.Users on r.UserId equals u.Id into userJoin
                        from u in userJoin.DefaultIfEmpty()
                        join o in _context.Organizations on u.OrganizationId equals o.Id into orgJoin
                        from o in orgJoin.DefaultIfEmpty()
                        select new ReportListItem
                        {
                            Id = r.Id,
                            CreatedAt = r.CreatedAt,
                            Sender = u != null ? (u.Email ?? u.UserName) : "(unknown)",
                            OrganizationName = o != null ? o.Name : null,
                            ObstacleType = r.ObstacleType,
                            Status = r.Status,
                            ObstacleLocation = r.ObstacleLocation
                        };

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "id" => sortOrder == "asc" ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id),
                "createdat" => sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
                "sender" => sortOrder == "asc" ? query.OrderBy(r => r.Sender) : query.OrderByDescending(r => r.Sender),
                "organizationname" => sortOrder == "asc" ? query.OrderBy(r => r.OrganizationName ?? "") : query.OrderByDescending(r => r.OrganizationName ?? ""),
                "obstacletype" => sortOrder == "asc" ? query.OrderBy(r => r.ObstacleType ?? "") : query.OrderByDescending(r => r.ObstacleType ?? ""),
                "status" => sortOrder == "asc" ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var items = await query.ToListAsync();

            // Apply search filter in-memory
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                items = items.Where(r =>
                    r.Id.ToString().Contains(searchLower) ||
                    (r.Sender ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                    (r.OrganizationName ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                    ((r.ObstacleType ?? string.Empty).ToLowerInvariant().Contains(searchLower)) ||
                    r.CreatedAt.ToString("MMM dd, yyyy").ToLowerInvariant().Contains(searchLower) ||
                    ((r.Status ?? string.Empty).ToLowerInvariant().Contains(searchLower))
                ).ToList();
            }

            ViewBag.Organization = organization;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Search = search;

            return View(items);
        }

        private bool OrganizationExists(int id)
        {
            return _context.Organizations.Any(e => e.Id == id);
        }
    }
}

