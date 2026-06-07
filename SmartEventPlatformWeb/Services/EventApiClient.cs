using System.Net;
using System.Net.Http.Json;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatform.Contracts.Speakers;

namespace SmartEventPlatformWeb.Services;

public class EventApiClient : IEventApiClient
{
    private readonly HttpClient _httpClient;

    public EventApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("EventService");
    }

    public async Task<List<EventDto>> GetEventsAsync()
        => await GetListAsync<EventDto>("api/events");

    public async Task<EventDto?> GetEventByIdAsync(long id)
        => await GetNullableAsync<EventDto>($"api/events/{id}");

    public async Task<EventRegistrationInfoDto?> GetEventRegistrationInfoAsync(long id)
        => await GetNullableAsync<EventRegistrationInfoDto>($"api/events/{id}/registration-info");

    public async Task<long> CreateEventAsync(EventCreateUpdateDto dto)
        => await PostAndReadIdAsync("api/events", dto);

    public async Task UpdateEventAsync(long id, EventCreateUpdateDto dto)
        => await PutAsync($"api/events/{id}", dto);

    public async Task DeleteEventAsync(long id)
        => await DeleteAsync($"api/events/{id}");

    public async Task<List<LocationDto>> GetLocationsAsync()
        => await GetListAsync<LocationDto>("api/locations");

    public async Task<LocationDto?> GetLocationByIdAsync(long id)
        => await GetNullableAsync<LocationDto>($"api/locations/{id}");

    public async Task<long> CreateLocationAsync(LocationDto dto)
        => await PostAndReadIdAsync("api/locations", dto);

    public async Task UpdateLocationAsync(long id, LocationDto dto)
        => await PutAsync($"api/locations/{id}", dto);

    public async Task DeleteLocationAsync(long id)
        => await DeleteAsync($"api/locations/{id}");

    public async Task<List<EventTypeDto>> GetEventTypesAsync()
        => await GetListAsync<EventTypeDto>("api/eventtypes");

    public async Task<EventTypeDto?> GetEventTypeByIdAsync(long id)
        => await GetNullableAsync<EventTypeDto>($"api/eventtypes/{id}");

    public async Task<long> CreateEventTypeAsync(EventTypeDto dto)
        => await PostAndReadIdAsync("api/eventtypes", dto);

    public async Task UpdateEventTypeAsync(long id, EventTypeDto dto)
        => await PutAsync($"api/eventtypes/{id}", dto);

    public async Task DeleteEventTypeAsync(long id)
        => await DeleteAsync($"api/eventtypes/{id}");

    public async Task<List<SpeakerDto>> GetSpeakersAsync()
        => await GetListAsync<SpeakerDto>("api/speakers");

    public async Task<SpeakerDto?> GetSpeakerByIdAsync(long id)
        => await GetNullableAsync<SpeakerDto>($"api/speakers/{id}");

    public async Task<long> CreateSpeakerAsync(SpeakerDto dto)
        => await PostAndReadIdAsync("api/speakers", dto);

    public async Task UpdateSpeakerAsync(long id, SpeakerDto dto)
        => await PutAsync($"api/speakers/{id}", dto);

    public async Task DeleteSpeakerAsync(long id)
        => await DeleteAsync($"api/speakers/{id}");

    public async Task<List<EventSpeakerDto>> GetEventSpeakersAsync()
        => await GetListAsync<EventSpeakerDto>("api/eventspeakers");

    public async Task<EventSpeakerDto?> GetEventSpeakerByIdAsync(long id)
        => await GetNullableAsync<EventSpeakerDto>($"api/eventspeakers/{id}");

    public async Task<long> CreateEventSpeakerAsync(EventSpeakerCreateUpdateDto dto)
        => await PostAndReadIdAsync("api/eventspeakers", dto);

    public async Task UpdateEventSpeakerAsync(long id, EventSpeakerCreateUpdateDto dto)
        => await PutAsync($"api/eventspeakers/{id}", dto);

    public async Task DeleteEventSpeakerAsync(long id)
        => await DeleteAsync($"api/eventspeakers/{id}");

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