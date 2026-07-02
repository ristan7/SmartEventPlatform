using SmartEventPlatform.EventService.CQRS.ReadModels;
using SmartEventPlatform.EventService.CQRS.Repositories;

namespace SmartEventPlatform.EventService.CQRS.Queries
{
    public class GetUpcomingEventsQueryHandler
    {
        private readonly IEventReadRepository _readRepository;

        public GetUpcomingEventsQueryHandler(IEventReadRepository readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<EventReadModel>> Handle(GetUpcomingEventsQuery query)
        {
            var fromDate = query.FromDate ?? DateTime.UtcNow.Date;

            return await _readRepository.GetUpcomingAsync(fromDate);
        }
    }
}