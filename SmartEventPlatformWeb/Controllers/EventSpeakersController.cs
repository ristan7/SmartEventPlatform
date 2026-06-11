using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatformWeb.ViewModels.EventSpeakers;
using System.Net;
using System.Net.Http.Json;
using SmartEventPlatformWeb.Infrastructure;

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
            try
            {
                var client = CreateEventServiceClient();
                //var eventSpeakers = await GetListAsync<EventSpeakerDto>(client, "api/eventspeakers");
                var eventSpeakers = await ApiHttpHelper.GetListAsync<EventSpeakerDto>(client, "api/eventspeakers");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading event speaker assignments. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading event speaker assignments.");
            }

            return View(new List<EventSpeakerListViewModel>());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var client = CreateEventServiceClient();
                //var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");
                var eventSpeaker = await ApiHttpHelper.GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading event speaker assignment details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading event speaker assignment details.");
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create()
        {
            var vm = new EventSpeakerCreateViewModel
            {
                Time = DateTime.Now
            };

            try
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading event speaker form data. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading event speaker form data.");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventSpeakerCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEventSpeakerCreateFormListsAsync(vm);
                return View(vm);
            }

            try
            {
                var isTimeValid = await IsSpeakerTimeInsideEventAsync(vm.EventId, vm.Time);

                if (!isTimeValid)
                {
                    ModelState.AddModelError(nameof(vm.Time),
                        "Speaker time must be within the selected event duration.");

                    await PopulateEventSpeakerCreateFormListsAsync(vm);
                    return View(vm);
                }

                var dto = new EventSpeakerCreateUpdateDto
                {
                    EventId = vm.EventId,
                    SpeakerId = vm.SpeakerId,
                    Topic = vm.Topic,
                    Time = vm.Time
                };

                var client = CreateEventServiceClient();
                //await PostAndReadIdAsync(client, "api/eventspeakers", dto);
                var result = await ApiHttpHelper.PostAndReadIdAsync(client, "api/eventspeakers", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event speaker assignment could not be created.");
                    await PopulateEventSpeakerCreateFormListsAsync(vm);
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the event speaker assignment. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the event speaker assignment.");
            }

            await PopulateEventSpeakerCreateFormListsAsync(vm);
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
                //var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");
                var eventSpeaker = await ApiHttpHelper.GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the event speaker assignment. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the event speaker assignment.");
            }

            return RedirectToAction(nameof(Index));
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
                await PopulateEventSpeakerEditFormListsAsync(vm);
                return View(vm);
            }

            try
            {
                var isTimeValid = await IsSpeakerTimeInsideEventAsync(vm.EventId, vm.Time);

                if (!isTimeValid)
                {
                    ModelState.AddModelError(nameof(vm.Time),
                        "Speaker time must be within the selected event duration.");

                    await PopulateEventSpeakerEditFormListsAsync(vm);
                    return View(vm);
                }

                var dto = new EventSpeakerCreateUpdateDto
                {
                    EventId = vm.EventId,
                    SpeakerId = vm.SpeakerId,
                    Topic = vm.Topic,
                    Time = vm.Time
                };

                var client = CreateEventServiceClient();
                //await PutAsync(client, $"api/eventspeakers/{id}", dto);
                var result = await ApiHttpHelper.PutAsync(client, $"api/eventspeakers/{id}", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event speaker assignment could not be updated.");
                    await PopulateEventSpeakerEditFormListsAsync(vm);
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the event speaker assignment. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the event speaker assignment.");
            }

            await PopulateEventSpeakerEditFormListsAsync(vm);
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
                //var eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");
                var eventSpeaker = await ApiHttpHelper.GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id.Value}");

                if (eventSpeaker == null)
                {
                    return NotFound();
                }

                var vm = MapToDeleteViewModel(eventSpeaker);

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the event speaker assignment. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the event speaker assignment.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            EventSpeakerDto? eventSpeaker = null;

            try
            {
                var client = CreateEventServiceClient();

                //eventSpeaker = await GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id}");
                eventSpeaker = await ApiHttpHelper.GetNullableAsync<EventSpeakerDto>(client, $"api/eventspeakers/{id}");

                if (eventSpeaker == null)
                {
                    return NotFound();
                }

                //await DeleteAsync(client, $"api/eventspeakers/{id}");
                var result = await ApiHttpHelper.DeleteAsync(client, $"api/eventspeakers/{id}");

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event speaker assignment could not be deleted.");

                    var deleteVm = MapToDeleteViewModel(eventSpeaker);
                    return View("Delete", deleteVm);
                }


                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the event speaker assignment. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the event speaker assignment.");
            }

            if (eventSpeaker == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = MapToDeleteViewModel(eventSpeaker);
            return View("Delete", vm);
        }

        private async Task<bool> IsSpeakerTimeInsideEventAsync(long eventId, DateTime speakerTime)
        {
            var client = CreateEventServiceClient();
            //var selectedEvent = await GetNullableAsync<EventDto>(client, $"api/events/{eventId}");
            var selectedEvent = await ApiHttpHelper.GetNullableAsync<EventDto>(client, $"api/events/{eventId}");

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
            //var events = await GetListAsync<EventDto>(client, "api/events");
            var events = await ApiHttpHelper.GetListAsync<EventDto>(client, "api/events");

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
            var client = CreateDirectoryServiceClient();
            //var speakers = await GetListAsync<SpeakerDto>(client, "api/speakers");
            var speakers = await ApiHttpHelper.GetListAsync<SpeakerDto>(client, "api/speakers");

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

        private async Task PopulateEventSpeakerCreateFormListsAsync(EventSpeakerCreateViewModel vm)
        {
            try
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
            }
            catch
            {
                vm.Events = new List<SelectListItem>();
                vm.Speakers = new List<SelectListItem>();
            }
        }

        private async Task PopulateEventSpeakerEditFormListsAsync(EventSpeakerEditViewModel vm)
        {
            try
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
            }
            catch
            {
                vm.Events = new List<SelectListItem>();
                vm.Speakers = new List<SelectListItem>();
            }
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

        private HttpClient CreateDirectoryServiceClient()
        {
            return _httpClientFactory.CreateClient("DirectoryService");
        }

        
    }
}