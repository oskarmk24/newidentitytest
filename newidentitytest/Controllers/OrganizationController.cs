using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using newidentitytest.Data;
using newidentitytest.Models;

namespace newidentitytest.Controllers
{
    /// <summary>
    /// Controller for administrasjon av organisasjoner.
    /// Støtter CRUD-operasjoner for organisasjoner og visning av rapporter fra organisasjonsmedlemmer.
    /// Forskjellige operasjoner krever ulike roller: Create/Edit krever Admin/Registrar/OrganizationManager,
    /// Delete krever Admin/Registrar, mens visning av rapporter har kompleks tilgangskontroll.
    /// </summary>
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initialiserer controlleren med ApplicationDbContext for databaseoperasjoner.
        /// </summary>
        public OrganizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Viser liste over alle organisasjoner med tilhørende brukere.
        /// Sortert alfabetisk etter navn. Tilgjengelig for alle autentiserte brukere.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var organizations = await _context.Organizations
                .Include(o => o.Users)
                .OrderBy(o => o.Name)
                .ToListAsync();
            return View(organizations);
        }

        /// <summary>
        /// Viser detaljvisning av en spesifikk organisasjon inkludert alle tilhørende brukere.
        /// Returnerer NotFound hvis organisasjonen ikke finnes.
        /// </summary>
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

        /// <summary>
        /// Viser skjema for å opprette ny organisasjon.
        /// </summary>
        [Authorize(Roles = "Admin,Registrar,OrganizationManager")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Oppretter en ny organisasjon basert på skjemadata.
        /// Validerer modellen og viser feilmeldinger hvis validering feiler.
        /// Ved suksess: redirecter til Index med suksessmelding.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Registrar,OrganizationManager")]
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

        /// <summary>
        /// Viser skjema for redigering av eksisterende organisasjon.
        /// Returnerer NotFound hvis organisasjonen ikke finnes.
        /// </summary>
        [Authorize(Roles = "Admin,Registrar,OrganizationManager")]
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

        /// <summary>
        /// Oppdaterer en eksisterende organisasjon basert på skjemadata.
        /// Håndterer DbUpdateConcurrencyException hvis organisasjonen har blitt endret av en annen bruker.
        /// Validerer modellen og viser feilmeldinger hvis validering feiler.
        /// Ved suksess: redirecter til Index med suksessmelding.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Registrar,OrganizationManager")]
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

        /// <summary>
        /// Viser bekreftelsesside for sletting av organisasjon.
        /// Inkluderer organisasjonens brukere i visningen for å vise konsekvenser av sletting.
        /// Returnerer NotFound hvis organisasjonen ikke finnes.
        /// </summary>
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

        /// <summary>
        /// Sletter organisasjonen permanent fra databasen.
        /// Brukere som tilhører organisasjonen får OrganizationId satt til null (via DeleteBehavior.SetNull).
        /// Ved suksess: redirecter til Index med suksessmelding.
        /// </summary>
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

        /// <summary>
        /// Viser liste over rapporter fra medlemmer av en spesifikk organisasjon.
        /// Har kompleks tilgangskontroll: Admin og Registrar kan se alle organisasjonsrapporter,
        /// OrganizationManager kan kun se rapporter fra sin egen organisasjon,
        /// og vanlige brukere kan se rapporter fra sin egen organisasjon.
        /// Støtter sortering (id, CreatedAt, Sender, OrganizationName, ObstacleType, Status)
        /// og søkefunksjonalitet. Returnerer Forbid hvis brukeren ikke har tilgang.
        /// </summary>
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

            // Sjekk at bruker er autorisert til å se rapporter for denne organisasjonen
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            bool hasAccess = false;
            if (user != null)
            {
                // Admin and Registerfører kan se alle organisasjonsrapporter
                if (User.IsInRole("Admin") || User.IsInRole("Registrar"))
                {
                    hasAccess = true;
                }
                // OrganizationManager ser sine egen organisasjons rapporter
                else if (User.IsInRole("OrganizationManager") && user.OrganizationId == id)
                {
                    hasAccess = true;
                }
                // Brukere ser sine egen organisasjons rapporter
                else if (user.OrganizationId == id)
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                return Forbid();
            }

            // Henter bruker-IDer for medlemmer av organisasjonen
            var organizationUserIds = await _context.Users
                .Where(u => u.OrganizationId == id)
                .Select(u => u.Id)
                .ToListAsync();

            // Lager spørring for rapporter fra disse brukerne
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

            // Sortering
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

            // Søkefunksjonalitet
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

        /// <summary>
        /// Hjelpemetode som sjekker om en organisasjon med gitt ID eksisterer i databasen.
        /// Brukes for concurrency-sjekk i Edit-metoden.
        /// </summary>
        private bool OrganizationExists(int id)
        {
            return _context.Organizations.Any(e => e.Id == id);
        }
    }
}

