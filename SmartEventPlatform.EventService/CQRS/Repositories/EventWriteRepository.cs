using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Messaging;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    public class EventWriteRepository : IEventWriteRepository
    {
        private readonly EventDbContext _context;

        public EventWriteRepository(EventDbContext context)
        {
            _context = context;
        }

        public async Task<Event?> GetByIdForWriteAsync(long id)
        {
            return await _context.Events
                .Include(e => e.EventSpeakers)
                .FirstOrDefaultAsync(e => e.EventId == id);
        }

        public async Task<long> CreateAsync(Event entity, Func<long, OutboxMessage> outboxFactory)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Korak 1: snimamo entitet da dobijemo auto-generisani EventId
                _context.Events.Add(entity);
                await _context.SaveChangesAsync();

                // Korak 2: sad znamo entity.EventId → factory pravi ispravan payload
                var outboxMsg = outboxFactory(entity.EventId);
                outboxMsg.CreatedAt = DateTime.UtcNow;
                _context.OutboxMessages.Add(outboxMsg);

                // Korak 3: sve u istoj transakciji — Outbox garantuje isporuku
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return entity.EventId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Event entity, IEnumerable<OutboxMessage> outboxMessages)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();

                foreach (var msg in outboxMessages)
                {
                    msg.CreatedAt = DateTime.UtcNow;
                    _context.OutboxMessages.Add(msg);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(Event entity, OutboxMessage locationNotification)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Events.Remove(entity);
                await _context.SaveChangesAsync();

                locationNotification.CreatedAt = DateTime.UtcNow;
                _context.OutboxMessages.Add(locationNotification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> EventTypeExistsAsync(long eventTypeId)
            => await _context.EventTypes.AnyAsync(et => et.EventTypeId == eventTypeId);

        public async Task<bool> HasRegistrationsAsync(long eventId)
            => await _context.EventRegistrationTrackers
                .AnyAsync(t => t.EventId == eventId && t.RegistrationCount > 0);
    }
}