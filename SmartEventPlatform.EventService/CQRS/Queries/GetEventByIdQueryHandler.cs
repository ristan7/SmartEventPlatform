using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.EventService.CQRS.Repositories;

namespace SmartEventPlatform.EventService.CQRS.Queries
{
    public class GetEventByIdQueryHandler
    {
        private readonly IEventReadRepository _readRepository;

        public GetEventByIdQueryHandler(IEventReadRepository readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<EventDto?> Handle(GetEventByIdQuery query)
        {
            return await _readRepository.GetByIdAsync(query.EventId);
        }
    }
}