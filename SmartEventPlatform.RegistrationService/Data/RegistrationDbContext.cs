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

        // Saga: tabela za praćenje stanja svakog Saga procesa
        public DbSet<SagaState> SagaStates { get; set; }

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

                // Saga: Status kolona - podrazumijevano "Confirmed" za kompatibilnost sa starim redovima
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

            // Saga: konfiguracija SagaState tabele
            modelBuilder.Entity<SagaState>(entity =>
            {
                entity.ToTable("SagaStates");
                entity.HasKey(e => e.SagaId);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // Indeksi za pretragu aktivnih saga po EventId i ParticipantId
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.RegistrationId);
            });
        }
    }
}