using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatform.Contracts.Registrations
{
    public class RegistrationCreateUpdateDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "Event is required.")]
        public long EventId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Participant is required.")]
        public long ParticipantId { get; set; }

        [Required(ErrorMessage = "Registration date is required.")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
