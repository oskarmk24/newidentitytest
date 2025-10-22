using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using newidentitytest.Models;

namespace newidentitytest.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Report> Reports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Report>(entity =>
        {
            entity.ToTable("reports", "obstacledb");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.ObstacleName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(r => r.ObstacleHeight);

            entity.Property(r => r.ObstacleDescription)
                  .HasColumnType("text");

            entity.Property(r => r.ObstacleLocation)
                  .HasColumnType("longtext");

            entity.Property(r => r.UserId)
                  .HasMaxLength(255);

            entity.Property(r => r.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

            entity.HasIndex(r => r.UserId)
                  .HasDatabaseName("IX_reports_UserId");
        });
    }
}
