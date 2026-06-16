using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.RegistrationService.Messaging;
using SmartEventPlatform.RegistrationService.Models;

namespace SmartEventPlatform.RegistrationService.Data
{
    public class RegistrationDbContext : DbContext
    {
        public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Participant> Participants { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Participant>(entity =>
            {
                entity.ToTable("Participants");
                entity.HasKey(e => e.ParticipantId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);

                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Registration>(entity =>
            {
                entity.ToTable("Registrations");
                entity.HasKey(r => r.RegistrationId);
                entity.Property(r => r.RegistrationDate).IsRequired().HasColumnType("datetime2");
                entity.Property(r => r.EventId).IsRequired();

                entity.HasOne(r => r.Participant)
                    .WithMany(p => p.Registrations)
                    .HasForeignKey(r => r.ParticipantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.EventId, r.ParticipantId }).IsUnique();
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.MessageId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EventType)
                    .IsRequired();

                entity.Property(e => e.Payload)
                    .IsRequired();

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.MessageId).IsUnique();
            });
        }
    }
}
