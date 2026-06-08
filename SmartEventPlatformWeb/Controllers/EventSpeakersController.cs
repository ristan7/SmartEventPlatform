using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatformWeb.ViewModels.EventSpeakers;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventSpeakersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventSpeakersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateEventServiceClient();
            var eventSpeakers = await GetListAsync<EventSpeakerDto>(client, "api/eventspeakers");

            var vm = eventSpeakers
                .OrderBy(es => es.EventName)
                .ThenBy(es => es.Time)
                .Select(es => new EventSpeakerListViewModel
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventName = es.EventName,
                    SpeakerName = es.SpeakerFullName,
                    Topic = es.Topic,
                    Time = es.Time,
                    EventId = es.EventId,
                    SpeakerId = es.SpeakerId
                })
                .ToList();

            return View(vm);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = CreateEventServiceClient();
            var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            var vm = new EventSpeakerDetailsViewModel
            {
                EventSpeakerId = eventSpeaker.EventSpeakerId,
                EventName = eventSpeaker.EventName,
                SpeakerFullName = eventSpeaker.SpeakerFullName,
                Topic = eventSpeaker.Topic,
                Time = eventSpeaker.Time,
                EventId = eventSpeaker.EventId,
                SpeakerId = eventSpeaker.SpeakerId
            };

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new EventSpeakerCreateViewModel
            {
                Time = DateTime.Now,
                Events = await GetEventsSelectListAsync(),
                Speakers = await GetSpeakersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventSpeakerCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
                return View(vm);
            }

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(vm.EventId, vm.Time);

            if (!isTimeValid)
            {
                ModelState.AddModelError(nameof(vm.Time),
                    "Speaker time must be within the selected event duration.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();

                return View(vm);
            }

            var dto = new EventSpeakerCreateUpdateDto
            {
                EventId = vm.EventId,
                SpeakerId = vm.SpeakerId,
                Topic = vm.Topic,
                Time = vm.Time
            };

            try
            {
                var client = CreateEventServiceClient();
                await PostAndReadIdAsync(client, "api/eventspeakers", dto);

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();

                return View(vm);
            }
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = CreateEventServiceClient();
            var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            var vm = new EventSpeakerEditViewModel
            {
                EventSpeakerId = eventSpeaker.EventSpeakerId,
                EventId = eventSpeaker.EventId,
                SpeakerId = eventSpeaker.SpeakerId,
                Topic = eventSpeaker.Topic,
                Time = eventSpeaker.Time,
                Events = await GetEventsSelectListAsync(),
                Speakers = await GetSpeakersSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EventSpeakerEditViewModel vm)
        {
            if (id != vm.EventSpeakerId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
                return View(vm);
            }

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(vm.EventId, vm.Time);

            if (!isTimeValid)
            {
                ModelState.AddModelError(nameof(vm.Time),
                    "Speaker time must be within the selected event duration.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();

                return View(vm);
            }

            var dto = new EventSpeakerCreateUpdateDto
            {
                EventId = vm.EventId,
                SpeakerId = vm.SpeakerId,
                Topic = vm.Topic,
                Time = vm.Time
            };

            try
            {
                var client = CreateEventServiceClient();
                await PutAsync(client, $"api/eventspeakers/{id}", dto);

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();

                return View(vm);
            }
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = CreateEventServiceClient();
            var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(eventSpeaker);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var client = CreateEventServiceClient();
            var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id}");

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            try
            {
                await DeleteAsync(client, $"api/eventspeakers/{id}");

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(eventSpeaker);

                return View("Delete", vm);
            }
        }

        private async Task<bool> IsSpeakerTimeInsideEventAsync(long eventId, DateTime speakerTime)
        {
            var client = CreateEventServiceClient();
            var selectedEvent = await GetNullableAsync<EventDto>(client, $"api/events/{eventId}");

            if (selectedEvent == null)
            {
                return false;
            }

            var eventStart = selectedEvent.EventDateTime;
            var eventEnd = selectedEvent.EventDateTime.AddMinutes(selectedEvent.DurationInMinutes);

            return speakerTime >= eventStart && speakerTime <= eventEnd;
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync()
        {
            var client = CreateEventServiceClient();
            var events = await GetListAsync<EventDto>(client, "api/events");

            return events
                .OrderBy(e => e.EventName)
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventName
                })
                .ToList();
        }

        private async Task<List<SelectListItem>> GetSpeakersSelectListAsync()
        {
            var client = CreateEventServiceClient();
            var speakers = await GetListAsync<SpeakerDto>(client, "api/speakers");

            return speakers
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new SelectListItem
                {
                    Value = s.SpeakerId.ToString(),
                    Text = s.FirstName + " " + s.LastName
                })
                .ToList();
        }

        private static EventSpeakerDeleteViewModel MapToDeleteViewModel(EventSpeakerDto eventSpeaker)
        {
            return new EventSpeakerDeleteViewModel
            {
                EventSpeakerId = eventSpeaker.EventSpeakerId,
                EventName = eventSpeaker.EventName,
                SpeakerFullName = eventSpeaker.SpeakerFullName,
                Topic = eventSpeaker.Topic,
                Time = eventSpeaker.Time
            };
        }

        private HttpClient CreateEventServiceClient()
        {
            return _httpClientFactory.CreateClient("EventService");
        }

        private static async Task<List<T>> GetListAsync<T>(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateApiExceptionAsync(response);
            }

            return await response.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
        }

        private static async Task<T?> GetNullableAsync<T>(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);

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

        private static async Task<long> PostAndReadIdAsync<T>(HttpClient client, string url, T dto)
        {
            var response = await client.PostAsJsonAsync(url, dto);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateApiExceptionAsync(response);
            }

            return await response.Content.ReadFromJsonAsync<long>();
        }

        private static async Task PutAsync<T>(HttpClient client, string url, T dto)
        {
            var response = await client.PutAsJsonAsync(url, dto);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateApiExceptionAsync(response);
            }
        }

        private static async Task DeleteAsync(HttpClient client, string url)
        {
            var response = await client.DeleteAsync(url);

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
}