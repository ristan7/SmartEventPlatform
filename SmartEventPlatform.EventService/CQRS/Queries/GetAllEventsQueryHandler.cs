using SmartEventPlatform.EventService.CQRS.ReadModels;
using SmartEventPlatform.EventService.CQRS.Repositories;

namespace SmartEventPlatform.EventService.CQRS.Queries
{
    /// <summary>
    /// Handler za GetAllEventsQuery.
    /// Ručna implementacija — controller direktno poziva Handle() metodu.
    /// </summary>
    public class GetAllEventsQueryHandler
    {
        private readonly IEventReadRepository _readRepository;

        public GetAllEventsQueryHandler(IEventReadRepository readRepository)
        {
            _readRepository = readRepository;
        }

        /// <summary>
        /// Izvršava upit. Query handler isključivo čita podatke — nema izmjena stanja.
        /// </summary>
        public async Task<List<EventReadModel>> Handle(GetAllEventsQuery query)
        {
            return await _readRepository.GetAllAsync();
        }
    }
}