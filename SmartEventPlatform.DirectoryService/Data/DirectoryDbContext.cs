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

        public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
        public DbSet<LocationUsageTracker> LocationUsageTrackers { get; set; }
        public DbSet<SpeakerUsageTracker> SpeakerUsageTrackers { get; set; }

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

            modelBuilder.Entity<ProcessedMessage>(entity =>
            {
                entity.ToTable("ProcessedMessages");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MessageId).IsUnique();
            });

            modelBuilder.Entity<LocationUsageTracker>(entity =>
            {
                entity.ToTable("LocationUsageTrackers");
                entity.HasKey(e => e.EventId);
                entity.Property(e => e.EventId)
                      .ValueGeneratedNever();

                entity.Property(e => e.LocationId)
                      .IsRequired();

                entity.HasIndex(e => e.LocationId);
            });

            modelBuilder.Entity<SpeakerUsageTracker>(entity =>
            {
                entity.ToTable("SpeakerUsageTrackers");
                entity.HasKey(e => e.EventSpeakerId);
                entity.Property(e => e.EventSpeakerId)
                      .ValueGeneratedNever();

                entity.Property(e => e.SpeakerId)
                      .IsRequired();

                entity.HasIndex(e => e.SpeakerId);
            });
        }
    }
}