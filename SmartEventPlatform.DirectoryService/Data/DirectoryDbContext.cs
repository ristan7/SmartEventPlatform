using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.DirectoryService.Models;

namespace SmartEventPlatform.DirectoryService.Data
{
    public class DirectoryDbContext : DbContext
    {
        public DirectoryDbContext(DbContextOptions<DirectoryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Location> Locations { get; set; }
        public DbSet<Speaker> Speakers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.LocationId);
                entity.Property(e => e.LocationName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(250);
                entity.Property(e => e.Capacity).IsRequired();
            });

            modelBuilder.Entity<Speaker>(entity =>
            {
                entity.ToTable("Speakers");
                entity.HasKey(e => e.SpeakerId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(150);
                entity.Property(e => e.ExpertiseAreas).HasMaxLength(500);
            });
        }
    }
}