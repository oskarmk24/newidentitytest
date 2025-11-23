using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Models;

namespace newidentitytest.Data;

/// <summary>
/// Entity Framework DbContext for applikasjonen.
/// Arver fra IdentityDbContext&lt;ApplicationUser&gt; for å støtte ASP.NET Core Identity med tilpasset brukermodell.
/// Håndterer databasetabeller for rapporter, organisasjoner, notifikasjoner og Identity-tabeller.
/// Konfigurerer relasjoner mellom entiteter, indekser og standardverdier.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Initialiserer en ny instans av ApplicationDbContext med de angitte alternativene.
    /// </summary>
    /// <param name="options">Alternativene for denne konteksten.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// DbSet for hinderrapporter.
    /// </summary>
    public DbSet<Report> Reports { get; set; } = null!;

    /// <summary>
    /// DbSet for organisasjoner. Støtter organisasjonshåndtering og relasjoner til brukere.
    /// </summary>
    public DbSet<Organization> Organizations { get; set; } = null!;

    /// <summary>
    /// DbSet for notifikasjoner. Brukes for in-app notifikasjoner til brukere.
    /// </summary>
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>
    /// Konfigurerer datamodellen ved å definere tabellnavn, nøkler, egenskaper, indekser og relasjoner.
    /// Kaller base.OnModelCreating for å konfigurere Identity-tabeller.
    /// </summary>
    /// <param name="builder">ModelBuilder for å konfigurere modellen.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Konfigurerer Organization-entiteten
        builder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(o => o.Description)
                  .HasMaxLength(500);

            entity.Property(o => o.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

            // Konfigurerer en-til-mange-relasjon: Organization -> Users
            // Hvis organisasjonen slettes, settes OrganizationId til null for tilknyttede brukere
            entity.HasMany(o => o.Users)
                  .WithOne(u => u.Organization)
                  .HasForeignKey(u => u.OrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Konfigurerer ApplicationUser-relasjonen til Organization
        // Dette er den inverse siden av Organization -> Users-relasjonen
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Organization)
                  .WithMany(o => o.Users)
                  .HasForeignKey(u => u.OrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Konfigurerer Report-entiteten
        builder.Entity<Report>(entity =>
        {
            // Tabellnavn: "reports" (schema er spesifisert i connection string)
            entity.ToTable("reports");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.ObstacleHeight);

            entity.Property(r => r.ObstacleDescription)
                  .HasColumnType("text");

            entity.Property(r => r.ObstacleLocation)
                  .HasColumnType("longtext");

            entity.Property(r => r.ObstacleType)
                  .HasMaxLength(100);

            entity.Property(r => r.UserId)
                  .HasMaxLength(255);
            
            entity.Property(r => r.AssignedRegistrarId)
                  .HasMaxLength(255);

            entity.Property(r => r.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

            // Indekser for forbedret søke- og join-ytelse
            entity.HasIndex(r => r.UserId)
                  .HasDatabaseName("IX_reports_UserId");
            
            entity.HasIndex(r => r.AssignedRegistrarId)
                  .HasDatabaseName("IX_reports_AssignedRegistrarId");
        });

        // Konfigurerer Notification-entiteten
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.HasKey(n => n.Id);

            entity.Property(n => n.UserId)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(n => n.ReportId)
                  .IsRequired();

            entity.Property(n => n.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(n => n.Message)
                  .HasColumnType("text");

            entity.Property(n => n.IsRead)
                  .IsRequired()
                  .HasDefaultValue(false);

            entity.Property(n => n.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

            // Indekser for forbedret søke- og join-ytelse
            entity.HasIndex(n => n.UserId)
                  .HasDatabaseName("IX_notifications_UserId");

            entity.HasIndex(n => n.ReportId)
                  .HasDatabaseName("IX_notifications_ReportId");

            // Sammensatt indeks for effektiv filtrering på uleste notifikasjoner per bruker
            entity.HasIndex(n => new { n.UserId, n.IsRead })
                  .HasDatabaseName("IX_notifications_UserId_IsRead");
        });
    }
}
