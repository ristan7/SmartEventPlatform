using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.Registrations;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.Registrations;

namespace SmartEventPlatformWeb.Controllers
{
    public class RegistrationsController : Controller
    {
        private readonly IRegistrationApiClient _registrationApiClient;
        private readonly IEventApiClient _eventApiClient;

        public RegistrationsController(
            IRegistrationApiClient registrationApiClient,
            IEventApiClient eventApiClient)
        {
            _registrationApiClient = registrationApiClient;
            _eventApiClient = eventApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var registrations = await _registrationApiClient.GetRegistrationsAsync();

            var vm = registrations
                .OrderBy(r => r.RegistrationDate)
                .Select(r => new RegistrationListViewModel
                {
                    RegistrationId = r.RegistrationId,
                    EventName = r.EventName,
                    ParticipantFullName = r.ParticipantFullName,
                    RegistrationDate = r.RegistrationDate
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

            var registration = await _registrationApiClient.GetRegistrationByIdAsync(id.Value);

            if (registration == null)
            {
                return NotFound();
            }

            var vm = new RegistrationDetailsViewModel
            {
                RegistrationId = registration.RegistrationId,
                EventName = registration.EventName,
                ParticipantFullName = registration.ParticipantFullName,
                RegistrationDate = registration.RegistrationDate
            };

            return View(vm);
        }

        public async Task<IActionResult> Create(long? eventId)
        {
            var vm = new RegistrationCreateViewModel
            {
                RegistrationDate = DateTime.Now,
                Events = await GetEventsSelectListAsync(eventId),
                Participants = await GetParticipantsSelectListAsync()
            };

            if (eventId.HasValue)
            {
                vm.EventId = eventId.Value;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistrationCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();
                return View(vm);
            }

            var dto = new RegistrationCreateUpdateDto
            {
                EventId = vm.EventId,
                ParticipantId = vm.ParticipantId,
                RegistrationDate = vm.RegistrationDate
            };

            try
            {
                await _registrationApiClient.CreateRegistrationAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();

                return View(vm);
            }
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _registrationApiClient.GetRegistrationByIdAsync(id.Value);

            if (registration == null)
            {
                return NotFound();
            }

            var vm = new RegistrationEditViewModel
            {
                RegistrationId = registration.RegistrationId,
                EventId = registration.EventId,
                ParticipantId = registration.ParticipantId,
                RegistrationDate = registration.RegistrationDate,
                Events = await GetEventsSelectListAsync(registration.EventId),
                Participants = await GetParticipantsSelectListAsync(registration.ParticipantId)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, RegistrationEditViewModel vm)
        {
            if (id != vm.RegistrationId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync(vm.EventId);
                vm.Participants = await GetParticipantsSelectListAsync(vm.ParticipantId);
                return View(vm);
            }

            var dto = new RegistrationCreateUpdateDto
            {
                EventId = vm.EventId,
                ParticipantId = vm.ParticipantId,
                RegistrationDate = vm.RegistrationDate
            };

            try
            {
                await _registrationApiClient.UpdateRegistrationAsync(id, dto);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                vm.Events = await GetEventsSelectListAsync(vm.EventId);
                vm.Participants = await GetParticipantsSelectListAsync(vm.ParticipantId);

                return View(vm);
            }
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _registrationApiClient.GetRegistrationByIdAsync(id.Value);

            if (registration == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(registration);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var registration = await _registrationApiClient.GetRegistrationByIdAsync(id);

            if (registration == null)
            {
                return NotFound();
            }

            try
            {
                await _registrationApiClient.DeleteRegistrationAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(registration);

                return View("Delete", vm);
            }
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync(long? selectedId = null)
        {
            var events = await _eventApiClient.GetEventsAsync();

            return events
                .OrderBy(e => e.EventDateTime)
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventName,
                    Selected = selectedId.HasValue && e.EventId == selectedId.Value
                })
                .ToList();
        }

        private async Task<List<SelectListItem>> GetParticipantsSelectListAsync(long? selectedId = null)
        {
            var participants = await _registrationApiClient.GetParticipantsAsync();

            return participants
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new SelectListItem
                {
                    Value = p.ParticipantId.ToString(),
                    Text = p.FirstName + " " + p.LastName,
                    Selected = selectedId.HasValue && p.ParticipantId == selectedId.Value
                })
                .ToList();
        }

        private static RegistrationDeleteViewModel MapToDeleteViewModel(RegistrationDto registration)
        {
            return new RegistrationDeleteViewModel
            {
                RegistrationId = registration.RegistrationId,
                EventName = registration.EventName,
                ParticipantFullName = registration.ParticipantFullName,
                RegistrationDate = registration.RegistrationDate
            };
        }
    }
}