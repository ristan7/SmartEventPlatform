using SmartEventPlatform.Contracts.Events;

namespace SmartEventPlatform.RegistrationService.Services
{
    public class EventServiceClient
    {
        private readonly HttpClient _httpClient;

        public EventServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EventRegistrationInfoDto?> GetRegistrationInfoAsync(long eventId)
        {
            return await _httpClient.GetFromJsonAsync<EventRegistrationInfoDto>(
                $"/api/events/{eventId}/registration-info");
        }

        public async Task<List<EventDto>> GetEventsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<EventDto>>("/api/events");
            return result ?? new List<EventDto>();
        }
    }
}
