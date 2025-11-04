using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using newidentitytest.Models;

namespace newidentitytest.Data;

public static class DatabaseSeeder
{
    public static async Task SeedRolesAndOrganizations(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        // Create roles if they don't exist
        string[] roles = { "Admin", "Registerfører", "Pilot" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        
        // Create default admin user if no admin exists
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
                // User exists but doesn't have Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Admin role assigned to existing user: {adminEmail}");
                }
            }
        }
        
        // Create sample organizations if none exist
        if (!dbContext.Organizations.Any())
        {
            dbContext.Organizations.AddRange(
                new Organization { Name = "Kartverket", Description = "Ansatte i Kartverket" },
                new Organization { Name = "NLA", Description = "Ansatte i Norsk luftambulanse" }
            );
            await dbContext.SaveChangesAsync();
        }
    }

    public static async Task SeedTestData(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var random = new Random();
        
        // Hent organisasjoner
        var kartverket = dbContext.Organizations.FirstOrDefault(o => o.Name == "Kartverket");
        var nla = dbContext.Organizations.FirstOrDefault(o => o.Name == "NLA");
        
        // ObstacleType-verdier (diverse typer for å teste "top 3" spørringen)
        var obstacleTypes = new[] { 
            "Bygning", "Telefontårn", "Vindkraftverk", "Kran", 
            "Mast", "Trær", "Bro", "Skiheis", "Silo", "Annet" 
        };
        
        // Opprett pilot-brukere (minst 10+)
        var pilotUsers = new List<ApplicationUser>();
        
        if (!dbContext.Reports.Any()) // Bare seed hvis det ikke finnes rapporter
        {
            for (int i = 1; i <= 15; i++)
            {
                var pilot = new ApplicationUser
                {
                    UserName = $"pilot{i}@example.com",
                    Email = $"pilot{i}@example.com",
                    EmailConfirmed = true,
                    OrganizationId = (i % 2 == 0) ? kartverket?.Id : nla?.Id
                };
                
                var result = await userManager.CreateAsync(pilot, "Pilot123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pilot, "Pilot");
                    pilotUsers.Add(pilot);
                    Console.WriteLine($"Created pilot user: pilot{i}@example.com");
                }
            }
            
            await dbContext.SaveChangesAsync();
            
            // Opprett rapporter med variert distribusjon
            var reports = new List<Report>();
            
            // Data fra 2023 (for gjennomsnittsberegning)
            var baseDate2023 = new DateTime(2023, 1, 1);
            for (int i = 0; i < 30; i++)
            {
                var pilot = pilotUsers[random.Next(pilotUsers.Count)];
                reports.Add(new Report
                {
                    ObstacleName = $"Hindring 2023-{i + 1}",
                    ObstacleType = obstacleTypes[random.Next(obstacleTypes.Length)],
                    ObstacleHeight = random.Next(10, 200),
                    ObstacleLocation = $"{{\"type\":\"Point\",\"coordinates\":[{59.91 + random.NextDouble() * 0.5},{10.75 + random.NextDouble() * 0.5}]}}",
                    ObstacleDescription = $"Beskrivelse av hindring {i + 1} fra 2023",
                    UserId = pilot.Id,
                    CreatedAt = baseDate2023.AddDays(random.Next(0, 365))
                });
            }
            
            // Data fra 2024
            var baseDate2024 = new DateTime(2024, 1, 1);
            for (int i = 0; i < 20; i++)
            {
                var pilot = pilotUsers[random.Next(pilotUsers.Count)];
                reports.Add(new Report
                {
                    ObstacleName = $"Hindring 2024-{i + 1}",
                    ObstacleType = obstacleTypes[random.Next(obstacleTypes.Length)],
                    ObstacleHeight = random.Next(10, 200),
                    ObstacleLocation = $"{{\"type\":\"Point\",\"coordinates\":[{59.91 + random.NextDouble() * 0.5},{10.75 + random.NextDouble() * 0.5}]}}",
                    ObstacleDescription = $"Beskrivelse av hindring {i + 1} fra 2024",
                    UserId = pilot.Id,
                    CreatedAt = baseDate2024.AddDays(random.Next(0, 365))
                });
            }
            
            // Data fra 2025 (for spørringene som krever 2025-data)
            var baseDate2025 = new DateTime(2025, 1, 1);
            var today2025 = DateTime.Now.Year == 2025 ? DateTime.Now : new DateTime(2025, 11, 3);
            
            // Gjør noen piloter mer aktive i 2025
            var activePilots = pilotUsers.Take(5).ToList();
            
            for (int i = 0; i < 50; i++)
            {
                var pilot = (random.NextDouble() < 0.6 && activePilots.Any()) 
                    ? activePilots[random.Next(activePilots.Count)]
                    : pilotUsers[random.Next(pilotUsers.Count)];
                
                reports.Add(new Report
                {
                    ObstacleName = $"Hindring 2025-{i + 1}",
                    ObstacleType = obstacleTypes[random.Next(obstacleTypes.Length)],
                    ObstacleHeight = random.Next(10, 200),
                    ObstacleLocation = $"{{\"type\":\"Point\",\"coordinates\":[{59.91 + random.NextDouble() * 0.5},{10.75 + random.NextDouble() * 0.5}]}}",
                    ObstacleDescription = $"Beskrivelse av hindring {i + 1} fra 2025",
                    UserId = pilot.Id,
                    CreatedAt = baseDate2025.AddDays(random.Next(0, (today2025 - baseDate2025).Days))
                });
            }
            
            // Legg til flere rapporter for noen piloter (mer enn 5 rapporter)
            for (int i = 0; i < 10; i++)
            {
                var pilot = pilotUsers[i % 3];
                reports.Add(new Report
                {
                    ObstacleName = $"Ekstra hindring {i + 1}",
                    ObstacleType = obstacleTypes[random.Next(obstacleTypes.Length)],
                    ObstacleHeight = random.Next(10, 200),
                    ObstacleLocation = $"{{\"type\":\"Point\",\"coordinates\":[{59.91 + random.NextDouble() * 0.5},{10.75 + random.NextDouble() * 0.5}]}}",
                    ObstacleDescription = $"Ekstra rapport for å teste spørringer",
                    UserId = pilot.Id,
                    CreatedAt = baseDate2025.AddDays(random.Next(0, (today2025 - baseDate2025).Days))
                });
            }
            
            // Legg til flere av de mest populære ObstacleType-ene (for "top 3" spørringen)
            var popularTypes = obstacleTypes.Take(3).ToArray();
            for (int i = 0; i < 20; i++)
            {
                var pilot = pilotUsers[random.Next(pilotUsers.Count)];
                reports.Add(new Report
                {
                    ObstacleName = $"Populær hindring {i + 1}",
                    ObstacleType = popularTypes[random.Next(popularTypes.Length)],
                    ObstacleHeight = random.Next(10, 200),
                    ObstacleLocation = $"{{\"type\":\"Point\",\"coordinates\":[{59.91 + random.NextDouble() * 0.5},{10.75 + random.NextDouble() * 0.5}]}}",
                    ObstacleDescription = $"Rapport med populær hindringstype",
                    UserId = pilot.Id,
                    CreatedAt = baseDate2025.AddDays(random.Next(0, (today2025 - baseDate2025).Days))
                });
            }
            
            dbContext.Reports.AddRange(reports);
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"Created {reports.Count} test reports");
        }
    }
}
