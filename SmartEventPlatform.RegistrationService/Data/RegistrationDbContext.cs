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

        // Saga Orkestracija
        public DbSet<SagaState> SagaStates { get; set; }

        // Saga Koreografija — NOVO
        public DbSet<SagaChoreographyState> SagaChoreographyStates { get; set; }

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
                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Confirmed");
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
                entity.Property(e => e.MessageId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EventType).IsRequired();
                entity.Property(e => e.Payload).IsRequired();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.MessageId).IsUnique();
            });

            // Saga Orkestracija
            modelBuilder.Entity<SagaState>(entity =>
            {
                entity.ToTable("SagaStates");
                entity.HasKey(e => e.SagaId);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FailureReason).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.RegistrationId);
            });

            // Saga Koreografija — NOVO
            modelBuilder.Entity<SagaChoreographyState>(entity =>
            {
                entity.ToTable("SagaChoreographyStates");
                entity.HasKey(e => e.SagaId);
                entity.Property(e => e.CorrelationId).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FailureReason).HasMaxLength(500);
                entity.Property(e => e.ParticipantFirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ParticipantLastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ParticipantEmail).IsRequired().HasMaxLength(200);
                entity.Property(e => e.EventName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasIndex(e => e.CorrelationId).IsUnique();
                entity.HasIndex(e => e.Status);
            });
        }
    }
}