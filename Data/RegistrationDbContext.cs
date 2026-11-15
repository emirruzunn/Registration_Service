using Microsoft.EntityFrameworkCore;
using RegistrationService.Models;

namespace RegistrationService.Data;

public class RegistrationDbContext : DbContext
{
    public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options) { }

    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Registration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            // Ayni kullanici ayni etkinlige iki kez kayit olamaz
            e.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
            e.HasIndex(x => x.EventId);
        });
    }
}
