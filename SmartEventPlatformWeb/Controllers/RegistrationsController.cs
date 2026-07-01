using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatform.Contracts.Registrations;
using SmartEventPlatformWeb.Infrastructure;
using SmartEventPlatformWeb.ViewModels.Registrations;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class RegistrationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegistrationsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = CreateClient();
                var registrations = await ApiHttpHelper.GetListAsync<RegistrationDto>(client, "gateway/registrations");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading registrations. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading registrations.");
            }

            return View(new List<RegistrationListViewModel>());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var registration = await ApiHttpHelper.GetNullableAsync<RegistrationDto>(client, $"gateway/registrations/{id.Value}");

                if (registration == null) return NotFound();

                var vm = new RegistrationDetailsViewModel
                {
                    RegistrationId = registration.RegistrationId,
                    EventName = registration.EventName,
                    ParticipantFullName = registration.ParticipantFullName,
                    RegistrationDate = registration.RegistrationDate
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading registration details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading registration details.");
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create(long? eventId)
        {
            var vm = new RegistrationCreateViewModel { RegistrationDate = DateTime.Now };

            if (eventId.HasValue) vm.EventId = eventId.Value;

            try
            {
                vm.Events = await GetEventsSelectListAsync(eventId);
                vm.Participants = await GetParticipantsSelectListAsync();
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
        public async Task<IActionResult> Create(RegistrationCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCreateFormListsAsync(vm);
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
                var client = CreateClient();
                var result = await ApiHttpHelper.PostAndReadIdAsync(client, "gateway/registrations", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The registration could not be created.");
                    await PopulateCreateFormListsAsync(vm);
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the registration. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the registration.");
            }

            await PopulateCreateFormListsAsync(vm);
            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var registration = await ApiHttpHelper.GetNullableAsync<RegistrationDto>(client, $"gateway/registrations/{id.Value}");

                if (registration == null) return NotFound();

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the registration. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the registration.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, RegistrationEditViewModel vm)
        {
            if (id != vm.RegistrationId) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEditFormListsAsync(vm);
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
                var client = CreateClient();
                var result = await ApiHttpHelper.PutAsync(client, $"gateway/registrations/{id}", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The registration could not be updated.");
                    await PopulateEditFormListsAsync(vm);
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the registration. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the registration.");
            }

            await PopulateEditFormListsAsync(vm);
            return View(vm);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var registration = await ApiHttpHelper.GetNullableAsync<RegistrationDto>(client, $"gateway/registrations/{id.Value}");

                if (registration == null) return NotFound();

                return View(MapToDeleteViewModel(registration));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the registration. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the registration.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            RegistrationDto? registration = null;

            try
            {
                var client = CreateClient();
                registration = await ApiHttpHelper.GetNullableAsync<RegistrationDto>(client, $"gateway/registrations/{id}");

                if (registration == null) return NotFound();

                var result = await ApiHttpHelper.DeleteAsync(client, $"gateway/registrations/{id}");

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The registration could not be deleted.");
                    return View("Delete", MapToDeleteViewModel(registration));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the registration. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the registration.");
            }

            if (registration == null) return RedirectToAction(nameof(Index));

            return View("Delete", MapToDeleteViewModel(registration));
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync(long? selectedId = null)
        {
            var client = CreateClient();
            var events = await ApiHttpHelper.GetListAsync<EventDto>(client, "gateway/events");

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
            var client = CreateClient();
            var participants = await ApiHttpHelper.GetListAsync<ParticipantDto>(client, "gateway/participants");

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

        private async Task PopulateCreateFormListsAsync(RegistrationCreateViewModel vm)
        {
            try
            {
                vm.Events = await GetEventsSelectListAsync(vm.EventId);
                vm.Participants = await GetParticipantsSelectListAsync(vm.ParticipantId);
            }
            catch
            {
                vm.Events = new List<SelectListItem>();
                vm.Participants = new List<SelectListItem>();
            }
        }

        private async Task PopulateEditFormListsAsync(RegistrationEditViewModel vm)
        {
            try
            {
                vm.Events = await GetEventsSelectListAsync(vm.EventId);
                vm.Participants = await GetParticipantsSelectListAsync(vm.ParticipantId);
            }
            catch
            {
                vm.Events = new List<SelectListItem>();
                vm.Participants = new List<SelectListItem>();
            }
        }

        private static RegistrationDeleteViewModel MapToDeleteViewModel(RegistrationDto r) =>
            new RegistrationDeleteViewModel
            {
                RegistrationId = r.RegistrationId,
                EventName = r.EventName,
                ParticipantFullName = r.ParticipantFullName,
                RegistrationDate = r.RegistrationDate
            };

        private HttpClient CreateClient() =>
            _httpClientFactory.CreateClient("ApiGateway");
    }
}