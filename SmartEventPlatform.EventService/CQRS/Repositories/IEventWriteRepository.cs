using SmartEventPlatform.EventService.Messaging;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    
    public interface IEventWriteRepository
    {
        Task<Event?> GetByIdForWriteAsync(long id);

        
        Task<long> CreateAsync(Event entity, Func<long, OutboxMessage> outboxFactory);

        Task UpdateAsync(Event entity, IEnumerable<OutboxMessage> outboxMessages);

        Task DeleteAsync(Event entity, OutboxMessage locationNotification);

        Task<bool> EventTypeExistsAsync(long eventTypeId);
        Task<bool> HasRegistrationsAsync(long eventId);
    }
}