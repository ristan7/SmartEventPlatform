using SmartEventPlatform.EventService.CQRS.ReadModels;
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

        public async Task<EventReadModel?> Handle(GetEventByIdQuery query)
        {
            return await _readRepository.GetByIdAsync(query.EventId);
        }
    }
}