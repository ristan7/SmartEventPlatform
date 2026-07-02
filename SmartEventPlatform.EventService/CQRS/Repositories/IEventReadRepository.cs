using SmartEventPlatform.EventService.CQRS.ReadModels;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    
    public interface IEventReadRepository
    {
        Task<List<EventReadModel>> GetAllAsync();
        Task<EventReadModel?> GetByIdAsync(long id);
        Task<List<EventReadModel>> GetUpcomingAsync(DateTime fromDate);
    }
}