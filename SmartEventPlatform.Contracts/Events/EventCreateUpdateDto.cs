using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatform.Contracts.Events
{
    public class EventCreateUpdateDto
    {
        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(200, ErrorMessage = "Event name cannot be longer than 200 characters.")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date and time is required.")]
        public DateTime EventDateTime { get; set; }

        [StringLength(2000, ErrorMessage = "Agenda cannot be longer than 2000 characters.")]
        public string Agenda { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than 0.")]
        public int DurationInMinutes { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Registration fee cannot be negative.")]
        public decimal RegistrationFee { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Location is required.")]
        public long LocationId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Event type is required.")]
        public long EventTypeId { get; set; }
    }
}
