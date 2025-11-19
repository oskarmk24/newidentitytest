using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Models;

namespace newidentitytest.Data;

// UPDATED: Changed from IdentityDbContext to IdentityDbContext<ApplicationUser> to support custom user with organization
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Report> Reports { get; set; } = null!;
    
    // ADDED: Organizations DbSet to support organization management
    public DbSet<Organization> Organizations { get; set; } = null!;
    
    // ADDED: Notifications DbSet for in-app notifications
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ADDED: Configure Organization entity
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

            // Configure one-to-many relationship: Organization -> Users
            entity.HasMany(o => o.Users)
                  .WithOne(u => u.Organization)
                  .HasForeignKey(u => u.OrganizationId)
                  .OnDelete(DeleteBehavior.SetNull); // Set OrganizationId to null if organization is deleted
        });

        // ADDED: Configure ApplicationUser -> Organization relationship
        builder.Entity<ApplicationUser>(entity =>
        {
            // Configure the foreign key relationship
            entity.HasOne(u => u.Organization)
                  .WithMany(o => o.Users)
                  .HasForeignKey(u => u.OrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Report>(entity =>
        {
            // CHANGED: Removed schema parameter "obstacledb" - MariaDB/MySQL don't support EF Core schemas.
            // The schema is ignored due to MySqlSchemaBehavior.Ignore in Program.cs, but removing it here
            // for consistency and clarity. The database name is already specified in the connection string.
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

            entity.Property(r => r.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

            entity.HasIndex(r => r.UserId)
                  .HasDatabaseName("IX_reports_UserId");
        });

        // ADDED: Configure Notification entity
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

            entity.HasIndex(n => n.UserId)
                  .HasDatabaseName("IX_notifications_UserId");

            entity.HasIndex(n => n.ReportId)
                  .HasDatabaseName("IX_notifications_ReportId");

            entity.HasIndex(n => new { n.UserId, n.IsRead })
                  .HasDatabaseName("IX_notifications_UserId_IsRead");
        });
    }
}
