namespace SmartEventPlatform.EventService.Services
{
    public class RegistrationServiceClient : IRegistrationServiceClient
    {
        private readonly HttpClient _httpClient;

        public RegistrationServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> EventHasRegistrationsAsync(long eventId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<bool>(
                    $"/api/registrations/exists-for-event/{eventId}");

                return result;
            }
            catch
            {
                return true;
            }
        }
    }
}
