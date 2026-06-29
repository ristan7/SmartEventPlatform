using SmartEventPlatform.EventService.Messaging;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    /// <summary>
    /// Repozitorij koji se koristi isključivo za izmjenu stanja baze (Command strana CQRS-a).
    /// </summary>
    public interface IEventWriteRepository
    {
        /// <summary>
        /// Učitava tracked EF entitet za write operacije (izmjena, brisanje).
        /// Nije Query — vraća se samo radi write svrhe.
        /// </summary>
        Task<Event?> GetByIdForWriteAsync(long id);

        /// <summary>
        /// Kreira novi događaj. outboxFactory prima dodijeljeni EventId
        /// i kreira OutboxMessage s ispravnim payload-om unutar iste transakcije.
        /// </summary>
        Task<long> CreateAsync(Event entity, Func<long, OutboxMessage> outboxFactory);

        /// <summary>Ažurira događaj i atomično upisuje Outbox poruke.</summary>
        Task UpdateAsync(Event entity, IEnumerable<OutboxMessage> outboxMessages);

        /// <summary>Briše događaj i atomično upisuje Outbox poruku.</summary>
        Task DeleteAsync(Event entity, OutboxMessage locationNotification);

        Task<bool> EventTypeExistsAsync(long eventTypeId);
        Task<bool> HasRegistrationsAsync(long eventId);
    }
}