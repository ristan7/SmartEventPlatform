using SmartEventPlatform.Contracts.Events;
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

        public async Task<List<EventDto>> Handle(GetAllEventsQuery query)
        {
            return await _readRepository.GetAllAsync();
        }
    }
}