using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatformWeb.ViewModels.Events;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = CreateEventServiceClient();
                var events = await GetListAsync<EventDto>(client, "api/events");

                var vm = events
                    .OrderBy(e => e.EventDateTime)
                    .Select(e => new EventListViewModel
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                        EventDateTime = e.EventDateTime,
                        LocationName = e.LocationName,
                        EventTypeName = e.EventTypeName
                    })
                    .ToList();

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading events. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading events.");
            }

            return View(new List<EventListViewModel>());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var eventClient = CreateEventServiceClient();

                var eventDto = await GetNullableAsync<EventDto>(eventClient, $"api/events/{id.Value}");

                if (eventDto == null)
                {
                    return NotFound();
                }

                var eventSpeakers = await GetListAsync<EventSpeakerDto>(eventClient, "api/eventspeakers");

                var vm = new EventDetailsViewModel
                {
                    EventId = eventDto.EventId,
                    EventName = eventDto.EventName,
                    Agenda = eventDto.Agenda,
                    EventDateTime = eventDto.EventDateTime,
                    DurationInMinutes = eventDto.DurationInMinutes,
                    RegistrationFee = eventDto.RegistrationFee,
                    LocationName = eventDto.LocationName,
                    LocationAddress = eventDto.LocationAddress,
                    EventTypeName = eventDto.EventTypeName,
                    Speakers = eventSpeakers
                        .Where(es => es.EventId == eventDto.EventId)
                        .OrderBy(es => es.Time)
                        .Select(es => new EventSpeakerItemViewModel
                        {
                            EventSpeakerId = es.EventSpeakerId,
                            SpeakerFullName = es.SpeakerFullName,
                            Topic = es.Topic,
                            Time = es.Time
                        })
                        .ToList()
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading event details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading event details.");
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create()
        {
            var vm = new EventCreateViewModel();

            try
            {
                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading form data. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading form data.");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEventFormListsAsync(vm);
                return View(vm);
            }

            var dto = new EventCreateUpdateDto
            {
                EventName = vm.EventName,
                Agenda = vm.Agenda,
                EventDateTime = vm.EventDateTime,
                DurationInMinutes = vm.DurationInMinutes,
                RegistrationFee = vm.RegistrationFee,
                LocationId = vm.LocationId,
                EventTypeId = vm.EventTypeId
            };

            try
            {
                var client = CreateEventServiceClient();
                await PostAndReadIdAsync(client, "api/events", dto);

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the event. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the event.");
            }

            await PopulateEventFormListsAsync(vm);
            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var client = CreateEventServiceClient();
                var eventDto = await GetNullableAsync<EventDto>(client, $"api/events/{id.Value}");

                if (eventDto == null)
                {
                    return NotFound();
                }

                var vm = new EventEditViewModel
                {
                    EventId = eventDto.EventId,
                    EventName = eventDto.EventName,
                    Agenda = eventDto.Agenda,
                    EventDateTime = eventDto.EventDateTime,
                    DurationInMinutes = eventDto.DurationInMinutes,
                    RegistrationFee = eventDto.RegistrationFee,
                    LocationId = eventDto.LocationId,
                    EventTypeId = eventDto.EventTypeId,
                    Locations = await GetLocationsSelectListAsync(),
                    EventTypes = await GetTypesSelectListAsync()
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the event. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the event.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EventEditViewModel vm)
        {
            if (id != vm.EventId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateEventFormListsAsync(vm);
                return View(vm);
            }

            var dto = new EventCreateUpdateDto
            {
                EventName = vm.EventName,
                Agenda = vm.Agenda,
                EventDateTime = vm.EventDateTime,
                DurationInMinutes = vm.DurationInMinutes,
                RegistrationFee = vm.RegistrationFee,
                LocationId = vm.LocationId,
                EventTypeId = vm.EventTypeId
            };

            try
            {
                var client = CreateEventServiceClient();
                await PutAsync(client, $"api/events/{id}", dto);

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the event. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the event.");
            }

            await PopulateEventFormListsAsync(vm);
            return View(vm);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var client = CreateEventServiceClient();
                var eventDto = await GetNullableAsync<EventDto>(client, $"api/events/{id.Value}");

                if (eventDto == null)
                {
                    return NotFound();
                }

                var vm = MapToDeleteViewModel(eventDto);

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the event. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the event.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            EventDto? eventDto = null;

            try
            {
                var client = CreateEventServiceClient();

                eventDto = await GetNullableAsync<EventDto>(client, $"api/events/{id}");

                if (eventDto == null)
                {
                    return NotFound();
                }

                await DeleteAsync(client, $"api/events/{id}");

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the event. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the event.");
            }

            if (eventDto == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = MapToDeleteViewModel(eventDto);
            return View("Delete", vm);
        }

        public async Task<IActionResult> Available()
        {
            try
            {
                var client = CreateRegistrationServiceClient();
                var availableEvents = await GetListAsync<AvailableEventDto>(client, "api/availableevents");

                var vm = availableEvents
                    .OrderBy(e => e.EventDateTime)
                    .Select(e => new AvailableEventViewModel
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                        Agenda = e.Agenda,
                        EventDateTime = e.EventDateTime,
                        DurationInMinutes = e.DurationInMinutes,
                        RegistrationFee = e.RegistrationFee,
                        LocationName = e.LocationName,
                        Capacity = e.Capacity,
                        RegisteredCount = e.RegisteredCount,
                        Speakers = e.Speakers
                    })
                    .ToList();

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading available events. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading available events.");
            }

            return View(new List<AvailableEventViewModel>());
        }

        private async Task<List<SelectListItem>> GetLocationsSelectListAsync()
        {
            var client = CreateEventServiceClient();
            var locations = await GetListAsync<LocationDto>(client, "api/locations");

            return locations
                .OrderBy(l => l.LocationName)
                .Select(l => new SelectListItem
                {
                    Value = l.LocationId.ToString(),
                    Text = l.LocationName
                })
                .ToList();
        }

        private async Task<List<SelectListItem>> GetTypesSelectListAsync()
        {
            var client = CreateEventServiceClient();
            var eventTypes = await GetListAsync<EventTypeDto>(client, "api/eventtypes");

            return eventTypes
                .OrderBy(et => et.Name)
                .Select(et => new SelectListItem
                {
                    Value = et.EventTypeId.ToString(),
                    Text = et.Name
                })
                .ToList();
        }

        private async Task PopulateEventFormListsAsync(EventCreateViewModel vm)
        {
            try
            {
                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();
            }
            catch
            {
                vm.Locations = new List<SelectListItem>();
                vm.EventTypes = new List<SelectListItem>();
            }
        }

        private async Task PopulateEventFormListsAsync(EventEditViewModel vm)
        {
            try
            {
                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();
            }
            catch
            {
                vm.Locations = new List<SelectListItem>();
                vm.EventTypes = new List<SelectListItem>();
            }
        }

        private static EventDeleteViewModel MapToDeleteViewModel(EventDto eventDto)
        {
            return new EventDeleteViewModel
            {
                EventId = eventDto.EventId,
                EventName = eventDto.EventName,
                EventDateTime = eventDto.EventDateTime,
                LocationName = eventDto.LocationName
            };
        }

        private HttpClient CreateEventServiceClient()
        {
            return _httpClientFactory.CreateClient("EventService");
        }

        private HttpClient CreateRegistrationServiceClient()
        {
            return _httpClientFactory.CreateClient("RegistrationService");
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