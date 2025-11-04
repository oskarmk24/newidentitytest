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
    
}