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
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Registration> Registrations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("EventTypes");
                entity.HasKey(e => e.EventTypeId);
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

            modelBuilder.Entity<Participant>(entity =>
            {
                entity.ToTable("Participants");
                entity.HasKey(e => e.ParticipantId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
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

                entity.HasOne(e => e.EventType)
                   .WithMany(et => et.Events)
                   .HasForeignKey(e => e.EventTypeId)
                   .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventSpeaker>(entity =>
            {
                entity.ToTable("EventSpeakers");
                entity.HasKey(es => es.EventSpeakerId);
                entity.Property(es => es.Time).IsRequired().HasColumnType("datetime2");
                entity.Property(es => es.Topic).IsRequired().HasMaxLength(350);

                entity.HasOne(es => es.Event)
                    .WithMany(ev => ev.EventSpeakers)
                    .HasForeignKey(es => es.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(es => es.Speaker)
                    .WithMany(s => s.EventSpeakers)
                    .HasForeignKey(es => es.SpeakerId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<Registration>(entity =>
            {
                entity.ToTable("Registrations");
                entity.HasKey(r => r.RegistrationId);
                entity.Property(r => r.RegistrationDate).IsRequired().HasColumnType("datetime2");

                entity.HasOne(r => r.Event)
                    .WithMany(e => e.Registrations)
                    .HasForeignKey(r => r.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Participant)
                    .WithMany(p => p.Registrations)
                    .HasForeignKey(r => r.ParticipantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.EventId, r.ParticipantId }).IsUnique();
            });
        }
    }
}
