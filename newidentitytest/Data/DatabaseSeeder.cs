using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using newidentitytest.Models;

namespace newidentitytest.Data;

/// <summary>
/// Statisk klasse for seeding av initial data i databasen.
/// Oppretter roller, organisasjoner og testbrukere ved første oppstart.
/// Kalles automatisk fra Program.cs etter migreringer.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Hovedmetode for seeding av roller, organisasjoner og testbrukere.
    /// Oppretter standardroller (Admin, Registrar, Pilot, OrganizationManager) hvis de ikke eksisterer.
    /// Oppretter standard admin-bruker hvis ingen admin-brukere finnes.
    /// Oppretter eksempelorganisasjoner (Kartverket, NLA, Luftforsvaret, Politiet) hvis ingen organisasjoner finnes.
    /// Kaller SeedTestUsersAsync for å opprette testbrukere.
    /// </summary>
    /// <param name="serviceProvider">ServiceProvider for å hente nødvendige tjenester (RoleManager, UserManager, DbContext, Configuration).</param>
    public static async Task SeedRolesAndOrganizations(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        // Oppretter standardroller hvis de ikke allerede eksisterer
        string[] roles = { "Admin", "Registrar", "Pilot", "OrganizationManager" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        
        // Oppretter standard admin-bruker hvis ingen admin-brukere finnes
        var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
        if (!adminUsers.Any())
        {
            var adminEmail = configuration["AdminUser:Email"] ?? "admin@example.com";
            var adminPassword = configuration["AdminUser:Password"] ?? "Admin123!";
            
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Default admin user created: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Brukeren eksisterer, men mangler Admin-rolle - tildel rollen
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Admin role assigned to existing user: {adminEmail}");
                }
            }
        }
        
        // Oppretter eksempelorganisasjoner hvis ingen organisasjoner finnes
        if (!dbContext.Organizations.Any())
        {
            dbContext.Organizations.AddRange(
                new Organization { Name = "Kartverket", Description = "Employees at Kartverket" },
                new Organization { Name = "NLA", Description = "Employees at Norsk luftambulanse" },
                new Organization { Name = "Luftforsvaret", Description = "Air Force personnel" },
                new Organization { Name = "Politiets helikoptertjeneste", Description = "Police helicopter service personnel" }
            );
            await dbContext.SaveChangesAsync();
        }

        // Oppretter testbrukere
        await SeedTestUsersAsync(userManager, dbContext);
    }

    /// <summary>
    /// Oppretter testbrukere for utvikling og testing.
    /// Oppretter registerførere, piloter og organisasjonsledere fordelt på ulike organisasjoner.
    /// Hvis en bruker allerede eksisterer, tildeles rollen hvis den mangler.
    /// Hvis organisasjonen ikke finnes, hoppes brukeren over.
    /// </summary>
    /// <param name="userManager">UserManager for å opprette og administrere brukere.</param>
    /// <param name="dbContext">ApplicationDbContext for å hente organisasjoner.</param>
    private static async Task SeedTestUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        // Henter organisasjoner fra databasen for å tildele til testbrukere
        var kartverket = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Name == "Kartverket");
        var nla = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Name == "NLA");
        var luftforsvaret = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Name == "Luftforsvaret");
        var politiet = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Name == "Politiets helikoptertjeneste");

        // Oppretter registerførere tilknyttet Kartverket
        var registrarUsers = new[]
        {
            new { Email = "registrar1@kartverket.no", Password = "Registrar123!", Organization = kartverket },
            new { Email = "registrar2@kartverket.no", Password = "Registrar123!", Organization = kartverket }
        };

        foreach (var userData in registrarUsers)
        {
            if (userData.Organization == null) continue;
            
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    OrganizationId = userData.Organization.Id
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Registrar");
                    Console.WriteLine($"Registrar user created: {userData.Email}");
                }
                else
                {
                    Console.WriteLine($"Failed to create registrar user {userData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else if (!await userManager.IsInRoleAsync(existingUser, "Registrar"))
            {
                existingUser.OrganizationId = userData.Organization.Id;
                await userManager.UpdateAsync(existingUser);
                await userManager.AddToRoleAsync(existingUser, "Registrar");
                Console.WriteLine($"Registrar role assigned to existing user: {userData.Email}");
            }
        }

        // Oppretter piloter tilknyttet NLA
        var nlaPilots = new[]
        {
            new { Email = "pilot.nla1@nla.no", Password = "Pilot123!", Organization = nla },
            new { Email = "pilot.nla2@nla.no", Password = "Pilot123!", Organization = nla }
        };

        foreach (var userData in nlaPilots)
        {
            if (userData.Organization == null) continue;
            
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    OrganizationId = userData.Organization.Id
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Pilot");
                    Console.WriteLine($"NLA pilot user created: {userData.Email}");
                }
                else
                {
                    Console.WriteLine($"Failed to create NLA pilot user {userData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else if (!await userManager.IsInRoleAsync(existingUser, "Pilot"))
            {
                existingUser.OrganizationId = userData.Organization.Id;
                await userManager.UpdateAsync(existingUser);
                await userManager.AddToRoleAsync(existingUser, "Pilot");
                Console.WriteLine($"Pilot role assigned to existing user: {userData.Email}");
            }
        }

        // Oppretter piloter tilknyttet Luftforsvaret
        var luftforsvaretPilots = new[]
        {
            new { Email = "pilot.luftforsvaret1@mil.no", Password = "Pilot123!", Organization = luftforsvaret },
            new { Email = "pilot.luftforsvaret2@mil.no", Password = "Pilot123!", Organization = luftforsvaret }
        };

        foreach (var userData in luftforsvaretPilots)
        {
            if (userData.Organization == null) continue;
            
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    OrganizationId = userData.Organization.Id
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Pilot");
                    Console.WriteLine($"Luftforsvaret pilot user created: {userData.Email}");
                }
                else
                {
                    Console.WriteLine($"Failed to create Luftforsvaret pilot user {userData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else if (!await userManager.IsInRoleAsync(existingUser, "Pilot"))
            {
                existingUser.OrganizationId = userData.Organization.Id;
                await userManager.UpdateAsync(existingUser);
                await userManager.AddToRoleAsync(existingUser, "Pilot");
                Console.WriteLine($"Pilot role assigned to existing user: {userData.Email}");
            }
        }

        // Oppretter piloter tilknyttet Politiet
        var politietPilots = new[]
        {
            new { Email = "pilot.politiet1@politiet.no", Password = "Pilot123!", Organization = politiet }
        };

        foreach (var userData in politietPilots)
        {
            if (userData.Organization == null) continue;
            
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    OrganizationId = userData.Organization.Id
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Pilot");
                    Console.WriteLine($"Politiet pilot user created: {userData.Email}");
                }
                else
                {
                    Console.WriteLine($"Failed to create Politiet pilot user {userData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else if (!await userManager.IsInRoleAsync(existingUser, "Pilot"))
            {
                existingUser.OrganizationId = userData.Organization.Id;
                await userManager.UpdateAsync(existingUser);
                await userManager.AddToRoleAsync(existingUser, "Pilot");
                Console.WriteLine($"Pilot role assigned to existing user: {userData.Email}");
            }
        }

        // Oppretter organisasjonsledere (én per organisasjon)
        var organizationManagers = new[]
        {
            new { Email = "manager@kartverket.no", Password = "Manager123!", Organization = kartverket },
            new { Email = "manager@nla.no", Password = "Manager123!", Organization = nla },
            new { Email = "manager@luftforsvaret.no", Password = "Manager123!", Organization = luftforsvaret },
            new { Email = "manager@politiet.no", Password = "Manager123!", Organization = politiet }
        };

        foreach (var userData in organizationManagers)
        {
            if (userData.Organization == null) continue;
            
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    OrganizationId = userData.Organization.Id
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "OrganizationManager");
                    Console.WriteLine($"OrganizationManager user created: {userData.Email}");
                }
                else
                {
                    Console.WriteLine($"Failed to create OrganizationManager user {userData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else if (!await userManager.IsInRoleAsync(existingUser, "OrganizationManager"))
            {
                existingUser.OrganizationId = userData.Organization.Id;
                await userManager.UpdateAsync(existingUser);
                await userManager.AddToRoleAsync(existingUser, "OrganizationManager");
                Console.WriteLine($"OrganizationManager role assigned to existing user: {userData.Email}");
            }
        }
    }
}