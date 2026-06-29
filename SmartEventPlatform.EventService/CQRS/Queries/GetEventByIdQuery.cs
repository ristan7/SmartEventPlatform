namespace SmartEventPlatform.EventService.CQRS.Queries
{
    /// <summary>
    /// Upit koji vraća jedan događaj po ID-u, ili null ako ne postoji.
    /// </summary>
    public class GetEventByIdQuery
    {
        public long EventId { get; set; }
    }
}