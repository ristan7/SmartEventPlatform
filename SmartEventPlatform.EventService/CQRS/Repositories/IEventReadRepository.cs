using SmartEventPlatform.EventService.CQRS.ReadModels;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    /// <summary>
    /// Repozitorij koji se koristi isključivo za čitanje podataka (Query strana CQRS-a).
    /// Metode ovog interfejsa NIKADA ne smiju mijenjati stanje baze podataka.
    /// </summary>
    public interface IEventReadRepository
    {
        Task<List<EventReadModel>> GetAllAsync();
        Task<EventReadModel?> GetByIdAsync(long id);
        Task<List<EventReadModel>> GetUpcomingAsync(DateTime fromDate);
    }
}