using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.Events;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventApiClient _eventApiClient;
        private readonly IRegistrationApiClient _registrationApiClient;

        public EventsController(
            IEventApiClient eventApiClient,
            IRegistrationApiClient registrationApiClient)
        {
            _eventApiClient = eventApiClient;
            _registrationApiClient = registrationApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _eventApiClient.GetEventsAsync();

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

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventDto = await _eventApiClient.GetEventByIdAsync(id.Value);

            if (eventDto == null)
            {
                return NotFound();
            }

            var eventSpeakers = await _eventApiClient.GetEventSpeakersAsync();

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

        public async Task<IActionResult> Create()
        {
            var vm = new EventCreateViewModel
            {
                Locations = await GetLocationsSelectListAsync(),
                EventTypes = await GetTypesSelectListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();
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
                await _eventApiClient.CreateEventAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();

                return View(vm);
            }
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventDto = await _eventApiClient.GetEventByIdAsync(id.Value);

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
                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();
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
                await _eventApiClient.UpdateEventAsync(id, dto);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                vm.Locations = await GetLocationsSelectListAsync();
                vm.EventTypes = await GetTypesSelectListAsync();

                return View(vm);
            }
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventDto = await _eventApiClient.GetEventByIdAsync(id.Value);

            if (eventDto == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(eventDto);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var eventDto = await _eventApiClient.GetEventByIdAsync(id);

            if (eventDto == null)
            {
                return NotFound();
            }

            try
            {
                await _eventApiClient.DeleteEventAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(eventDto);

                return View("Delete", vm);
            }
        }

        public async Task<IActionResult> Available()
        {
            try
            {
                var availableEvents = await _registrationApiClient.GetAvailableEventsAsync();

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
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                return View(new List<AvailableEventViewModel>());
            }
        }

        private async Task<List<SelectListItem>> GetLocationsSelectListAsync()
        {
            var locations = await _eventApiClient.GetLocationsAsync();

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
            var eventTypes = await _eventApiClient.GetEventTypesAsync();

            return eventTypes
                .OrderBy(et => et.Name)
                .Select(et => new SelectListItem
                {
                    Value = et.EventTypeId.ToString(),
                    Text = et.Name
                })
                .ToList();
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
    }
}