using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using newidentitytest.Data;             // <- ApplicationDbContext, ReportsRepository, osv.
using newidentitytest.Models;           // ADDED: For ApplicationUser
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1) Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2) EF Core + MariaDB (Pomelo) for Identity/DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(11, 4, 2)),
        mySql => mySql.SchemaBehavior(MySqlSchemaBehavior.Ignore)   // <-- MariaDB fix: ignore EF Core schemas
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 3) Identity (bruker EF store over ApplicationDbContext)
// UPDATED: Changed from IdentityUser to ApplicationUser to support organizations
// ADDED: .AddRoles<IdentityRole>() to enable role management functionality
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4) MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 6) Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Viktig for å serve wwwroot (f.eks. wwwroot/css/site.css fra Tailwind)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// REMOVED: MapStaticAssets() and WithStaticAssets() - These methods require .NET Aspire packages
// which are not included in this project. Static files are already handled by UseStaticFiles() above.
// If you need Aspire functionality, add the required NuGet packages.

// Bestemmer hvilken side som blir vist når prosjektet startes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapGet("/", () => Results.Redirect("/Home/Index"));

// Omdiriger rot-URL til innloggingssiden
//app.MapGet("/", () => Results.Redirect("/Identity/Account/Login"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    
    // ADDED: Seed initial roles and organizations
    await SeedRolesAndOrganizations(scope.ServiceProvider);
}

app.Run();

// ADDED: Seed method for initial roles and organizations
static async Task SeedRolesAndOrganizations(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    
    // Create roles if they don't exist
    string[] roles = { "Admin", "Manager", "User" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
    
    // ADDED: Create default admin user if no admin exists
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
                Console.WriteLine($"✓ Default admin user created: {adminEmail}");
            }
            else
            {
                Console.WriteLine($"✗ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // User exists but doesn't have Admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"✓ Admin role assigned to existing user: {adminEmail}");
            }
        }
    }
    
    // Create sample organizations if none exist
    if (!dbContext.Organizations.Any())
    {
        dbContext.Organizations.AddRange(
            new Organization { Name = "Acme Corp", Description = "Main organization" },
            new Organization { Name = "Tech Solutions", Description = "Technology department" }
        );
        await dbContext.SaveChangesAsync();
    }
}