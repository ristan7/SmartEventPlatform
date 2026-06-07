using System.Net;
using System.Net.Http.Json;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatform.Contracts.Registrations;

namespace SmartEventPlatformWeb.Services;

public class RegistrationApiClient : IRegistrationApiClient
{
    private readonly HttpClient _httpClient;

    public RegistrationApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("RegistrationService");
    }

    public async Task<List<ParticipantDto>> GetParticipantsAsync()
        => await GetListAsync<ParticipantDto>("api/participants");

    public async Task<ParticipantDto?> GetParticipantByIdAsync(long id)
        => await GetNullableAsync<ParticipantDto>($"api/participants/{id}");

    public async Task<long> CreateParticipantAsync(ParticipantDto dto)
        => await PostAndReadIdAsync("api/participants", dto);

    public async Task UpdateParticipantAsync(long id, ParticipantDto dto)
        => await PutAsync($"api/participants/{id}", dto);

    public async Task DeleteParticipantAsync(long id)
        => await DeleteAsync($"api/participants/{id}");

    public async Task<List<RegistrationDto>> GetRegistrationsAsync()
        => await GetListAsync<RegistrationDto>("api/registrations");

    public async Task<RegistrationDto?> GetRegistrationByIdAsync(long id)
        => await GetNullableAsync<RegistrationDto>($"api/registrations/{id}");

    public async Task<long> CreateRegistrationAsync(RegistrationCreateUpdateDto dto)
        => await PostAndReadIdAsync("api/registrations", dto);

    public async Task UpdateRegistrationAsync(long id, RegistrationCreateUpdateDto dto)
        => await PutAsync($"api/registrations/{id}", dto);

    public async Task DeleteRegistrationAsync(long id)
        => await DeleteAsync($"api/registrations/{id}");

    public async Task<List<AvailableEventDto>> GetAvailableEventsAsync()
        => await GetListAsync<AvailableEventDto>("api/availableevents");

    public async Task<bool> ExistsForEventAsync(long eventId)
    {
        var response = await _httpClient.GetAsync($"api/registrations/exists-for-event/{eventId}");

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
    }

    private async Task<T?> GetNullableAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<long> PostAndReadIdAsync<T>(string url, T dto)
    {
        var response = await _httpClient.PostAsJsonAsync(url, dto);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<long>();
    }

    private async Task PutAsync<T>(string url, T dto)
    {
        var response = await _httpClient.PutAsJsonAsync(url, dto);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }
    }

    private async Task DeleteAsync(string url)
    {
        var response = await _httpClient.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }
    }

    private static async Task<Exception> CreateApiExceptionAsync(HttpResponseMessage response)
    {
        var message = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"API request failed with status code {(int)response.StatusCode}.";
        }

        return new HttpRequestException(message, null, response.StatusCode);
    }
}