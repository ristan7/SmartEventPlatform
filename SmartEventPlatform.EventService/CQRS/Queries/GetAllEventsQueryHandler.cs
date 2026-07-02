using SmartEventPlatform.EventService.CQRS.ReadModels;
using SmartEventPlatform.EventService.CQRS.Repositories;

namespace SmartEventPlatform.EventService.CQRS.Queries
{
    
    public class GetAllEventsQueryHandler
    {
        private readonly IEventReadRepository _readRepository;

        public GetAllEventsQueryHandler(IEventReadRepository readRepository)
        {
            _readRepository = readRepository;
        }

        
        public async Task<List<EventReadModel>> Handle(GetAllEventsQuery query)
        {
            return await _readRepository.GetAllAsync();
        }
    }
}