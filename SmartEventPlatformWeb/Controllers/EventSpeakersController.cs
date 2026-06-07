using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.EventSpeakers;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventSpeakersController : Controller
    {
        private readonly IEventApiClient _eventApiClient;

        public EventSpeakersController(IEventApiClient eventApiClient)
        {
            _eventApiClient = eventApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var eventSpeakers = await _eventApiClient.GetEventSpeakersAsync();

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

            var eventSpeaker = await _eventApiClient.GetEventSpeakerByIdAsync(id.Value);

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
                await _eventApiClient.CreateEventSpeakerAsync(dto);
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

            var eventSpeaker = await _eventApiClient.GetEventSpeakerByIdAsync(id.Value);

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
                await _eventApiClient.UpdateEventSpeakerAsync(id, dto);
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

            var eventSpeaker = await _eventApiClient.GetEventSpeakerByIdAsync(id.Value);

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
            var eventSpeaker = await _eventApiClient.GetEventSpeakerByIdAsync(id);

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            try
            {
                await _eventApiClient.DeleteEventSpeakerAsync(id);
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
            var selectedEvent = await _eventApiClient.GetEventByIdAsync(eventId);

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
            var events = await _eventApiClient.GetEventsAsync();

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
            var speakers = await _eventApiClient.GetSpeakersAsync();

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
    }
}