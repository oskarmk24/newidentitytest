using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;                    // <- MySqlDataSourceBuilder
using newidentitytest.Data;             // <- ApplicationDbContext, ReportsRepository, osv.
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1) Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2) EF Core + MariaDB (Pomelo) for Identity/DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 4, 2))));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 3) Identity (bruker EF store over ApplicationDbContext)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4) MVC
builder.Services.AddControllersWithViews();

// 5) Dapper-infrastruktur via MySqlConnector (samme som i gamle prosjektet)
builder.Services.AddSingleton(sp => new MySqlDataSourceBuilder(connectionString).Build());
builder.Services.AddScoped<ReportsRepository>();

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

// Viktig for � serve wwwroot (f.eks. wwwroot/css/site.css fra Tailwind)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// (valgfritt) Aspire/WebAssets helpers � behold dem dersom du bruker dem i prosjektet
app.MapStaticAssets();

// Bestemmer hvilken side som blir vist n�r prosjektet startes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

// Omdiriger rot-URL til innloggingssiden
app.MapGet("/", () => Results.Redirect("/Identity/Account/Login"));


// liten sanity-check p� DB-tilkoblingen brukt av EF
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine($"Connected to: {db.Database.GetDbConnection().DataSource}");
    Console.WriteLine($"Database: {db.Database.GetDbConnection().Database}");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    // Optional: SeedAdmin(scope.ServiceProvider);  // hvis du vil lage default admin
}

app.Run();
