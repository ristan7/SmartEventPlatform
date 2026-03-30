namespace SmartEventPlatformWeb.ViewModels.Speakers
{
    public class SpeakerDetailsViewModel
    {
        public long SpeakerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExpertiseAreas { get; set; } = string.Empty;

        public List<SpeakerEventItemViewModel> EventSpeakersParticipations { get; set; } = new List<SpeakerEventItemViewModel>();
    }
}
