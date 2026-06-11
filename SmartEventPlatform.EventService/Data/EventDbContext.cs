using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.Data
{
    public class EventDbContext : DbContext
    {
        public EventDbContext(DbContextOptions<EventDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventSpeaker> EventSpeakers { get; set; }
        public DbSet<EventType> EventTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("EventTypes");
                entity.HasKey(e => e.EventTypeId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
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
                entity.Property(e => e.LocationId).IsRequired();
                entity.Property(e => e.LocationNameSnapshot).IsRequired().HasMaxLength(150);
                entity.Property(e => e.LocationAddressSnapshot).IsRequired().HasMaxLength(250);
                entity.Property(e => e.LocationCapacitySnapshot).IsRequired();

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
                entity.Property(es => es.SpeakerId).IsRequired();
                entity.Property(es => es.SpeakerFullNameSnapshot).IsRequired().HasMaxLength(250);

                entity.HasOne(es => es.Event)
                    .WithMany(ev => ev.EventSpeakers)
                    .HasForeignKey(es => es.EventId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
