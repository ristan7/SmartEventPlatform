using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatform.RegistrationService.Models
{
    public class SagaChoreographyStartRequest
    {
        [Required]
        public long EventId { get; set; }

        [Required]
        public long ParticipantId { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; }
    }
}