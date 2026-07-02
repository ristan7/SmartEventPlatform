using SmartEventPlatform.Contracts.Events;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    public interface IEventReadRepository
    {
        Task<List<EventDto>> GetAllAsync();
        Task<EventDto?> GetByIdAsync(long id);
        Task<List<EventDto>> GetUpcomingAsync(DateTime fromDate);
    }
}