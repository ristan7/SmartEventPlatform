namespace SmartEventPlatform.EventService.CQRS.Commands
{
    /// <summary>
    /// Komanda za brisanje događaja.
    /// Vraća true ako je pronađen i obrisan, false ako nije pronađen.
    /// Ako brisanje nije dozvoljeno (speakeri / registracije), baca InvalidOperationException.
    /// </summary>
    public class DeleteEventCommand
    {
        public long EventId { get; set; }
    }
}