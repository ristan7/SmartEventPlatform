using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatform.Contracts.EventSpeakers
{
    public class EventSpeakerCreateUpdateDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "Event is required.")]
        public long EventId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Speaker is required.")]
        public long SpeakerId { get; set; }

        [Required(ErrorMessage = "Topic is required.")]
        [StringLength(350, ErrorMessage = "Topic cannot be longer than 350 characters.")]
        public string Topic { get; set; } = string.Empty;

        [Required(ErrorMessage = "Presentation time is required.")]
        public DateTime Time { get; set; }
    }
}
