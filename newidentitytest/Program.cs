using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using newidentitytest.Data;             // ApplicationDbContext og database-tjenester
using newidentitytest.Models;           // ApplicationUser og domenemodeller
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

/// <summary>
/// Applikasjonens oppstartsfil og tjenestekonfigurasjon.
/// Konfigurerer Entity Framework Core med MariaDB, ASP.NET Core Identity,
/// MVC-tjenester og request pipeline middleware.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Henter database connection string fra konfigurasjon.
/// Kaster InvalidOperationException hvis connection string ikke finnes.
/// </summary>
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

/// <summary>
/// Konfigurerer Entity Framework Core med MariaDB ved hjelp av Pomelo provider.
/// Bruker MariaDB serverversjon 11.4.2 og ignorerer EF Core schema-oppførsel
/// for å sikre kompatibilitet med MariaDB.
/// </summary>
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(11, 4, 2)),
        mySql => mySql.SchemaBehavior(MySqlSchemaBehavior.Ignore)   // MariaDB-kompatibilitet: ignorer EF Core schemas
    ));

/// <summary>
/// Aktiverer developer exception page for database-relaterte feil i utviklingsmiljø.
/// </summary>
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

/// <summary>
/// Konfigurerer ASP.NET Core Identity med tilpasset ApplicationUser.
/// Bruker ApplicationUser i stedet for standard IdentityUser for å støtte organisasjonsrelasjoner.
/// Legger til rollehåndteringsfunksjonalitet med IdentityRole.
/// Deaktiverer e-postbekreftelse for kontologg inn.
/// </summary>
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

/// <summary>
/// Legger til MVC-tjenester med støtte for views.
/// </summary>
builder.Services.AddControllersWithViews();

var app = builder.Build();

/// <summary>
/// Konfigurerer request pipeline middleware.
/// I utviklingsmiljø: bruk migrations endpoint for databasehåndtering.
/// I produksjon: bruk exception handler og HTTP Strict Transport Security (HSTS).
/// </summary>
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

/// <summary>
/// Omdirigerer HTTP-forespørsler til HTTPS.
/// </summary>
app.UseHttpsRedirection();

/// <summary>
/// Aktiverer statisk fil-serving fra wwwroot-mappen.
/// Nødvendig for å serve CSS, JavaScript, bilder og andre statiske ressurser.
/// </summary>
app.UseStaticFiles();

/// <summary>
/// Aktiverer routing for å matche innkommende forespørsler til controllere og actions.
/// </summary>
app.UseRouting();

/// <summary>
/// Aktiverer autentiseringsmiddleware for å identifisere den nåværende brukeren.
/// </summary>
app.UseAuthentication();

/// <summary>
/// Aktiverer autorisasjonsmiddleware for å håndheve tilgangskontrollpolicyer.
/// Må kalles etter UseAuthentication() og UseRouting().
/// </summary>
app.UseAuthorization();

/// <summary>
/// Merknad: MapStaticAssets() og WithStaticAssets() krever .NET Aspire-pakker
/// som ikke er inkludert i dette prosjektet. Statiske filer håndteres av UseStaticFiles() over.
/// Hvis Aspire-funksjonalitet er nødvendig, legg til de nødvendige NuGet-pakkene.
/// </summary>

/// <summary>
/// Konfigurerer standard controller route-mønster.
/// Mapper forespørsler til controllere og actions: {controller}/{action}/{id?}
/// Standardiserer til Home/Index hvis ingen controller/action er spesifisert.
/// </summary>
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

/// <summary>
/// Mapper Razor Pages for Identity UI (innlogging, registrering, etc.).
/// </summary>
app.MapRazorPages();

/// <summary>
/// Omdirigerer rot-URL (/) til Home/Index.
/// </summary>
app.MapGet("/", () => Results.Redirect("/Home/Index"));

/// <summary>
/// Alternativ: Omdirigerer rot-URL til innloggingsside.
/// For øyeblikket kommentert ut - fjern kommentar for å omdirigere til innlogging i stedet for hjem.
/// </summary>
//app.MapGet("/", () => Results.Redirect("/Identity/Account/Login"));

/// <summary>
/// Anvender database-migrasjoner og seed initial data.
/// Oppretter en service scope for å få tilgang til ApplicationDbContext og DatabaseSeeder.
/// Kjører migrasjoner for å sikre at databaseskjemaet er oppdatert.
/// Seeder initiale roller (Admin, Registrar, Pilot, OrganizationManager) og organisasjoner.
/// </summary>
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    
    /// <summary>
    /// Seeder initiale roller og organisasjoner i databasen.
    /// </summary>
    await DatabaseSeeder.SeedRolesAndOrganizations(scope.ServiceProvider);
}

/// <summary>
/// Starter applikasjonen og begynner å lytte på HTTP-forespørsler.
/// </summary>
app.Run();