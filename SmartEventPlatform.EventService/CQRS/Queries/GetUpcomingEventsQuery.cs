namespace SmartEventPlatform.EventService.CQRS.Queries
{
    /// <summary>
    /// Upit koji vraća predstojeće događaje filtrirane po datumu.
    /// Demonstrira filtriranje podataka na Query strani CQRS-a.
    /// </summary>
    public class GetUpcomingEventsQuery
    {
        /// <summary>
        /// Minimalni datum. Vraćaju se eventi s EventDateTime >= FromDate.
        /// Ako je null, koristi se trenutni datum (od danas).
        /// </summary>
        public DateTime? FromDate { get; set; }
    }
}