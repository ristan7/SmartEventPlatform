using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Domains;

namespace SmartEventPlatformWeb.Data
{
    public class SmartPlatformDbContext : DbContext
    {
        public SmartPlatformDbContext(DbContextOptions<SmartPlatformDbContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Speaker> Speakers { get; set; }
        public DbSet<EventSpeaker> EventSpeakers { get; set; }
        public DbSet<EventRole> EventRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventRole>(entity =>
            {
                entity.ToTable("EventRoles");

                entity.HasKey(e => e.EventRoleId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

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

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events");

                entity.HasKey(e => e.EventId);
                entity.Property(e => e.EventName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Agenda).HasMaxLength(2000);
                entity.Property(e => e.EventDateTime).IsRequired().HasColumnType("datetime2");
                entity.Property(e => e.DurationInMinutes).IsRequired();
                entity.Property(e => e.RegistrationFee).IsRequired().HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Events)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<EventSpeaker>(entity =>
            {
                entity.ToTable("EventSpeakers");

                entity.HasKey(es => es.EventSpeakerId);
                entity.HasIndex(es => new { es.EventId, es.SpeakerId, es.Time }).IsUnique();
                entity.Property(es => es.Time).IsRequired().HasColumnType("datetime2");
                entity.Property(es => es.Topic).IsRequired().HasMaxLength(350);
                
                entity.HasOne(es => es.Event)
                    .WithMany(ev => ev.EventSpeakers)
                    .HasForeignKey(es => es.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(es => es.Speaker)
                    .WithMany(s => s.EventSpeakers)
                    .HasForeignKey(es => es.SpeakerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(es => es.EventRole)
                    .WithMany(e => e.EventSpeakers)
                    .HasForeignKey(es => es.EventRoleId)
                    .OnDelete(DeleteBehavior.Restrict);

            });
        }
    }
}
