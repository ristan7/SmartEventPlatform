using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatform.Contracts.Registrations;
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
                var client = CreateRegistrationServiceClient();
                var registrations = await GetListAsync<RegistrationDto>(client, "api/registrations");

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
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var client = CreateRegistrationServiceClient();
                var registration = await GetNullableAsync<RegistrationDto>(client, $"api/registrations/{id.Value}");

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
            var vm = new RegistrationCreateViewModel
            {
                RegistrationDate = DateTime.Now
            };

            if (eventId.HasValue)
            {
                vm.EventId = eventId.Value;
            }

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
                await PopulateRegistrationCreateFormListsAsync(vm);
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
                var client = CreateRegistrationServiceClient();
                await PostAndReadIdAsync(client, "api/registrations", dto);

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

            await PopulateRegistrationCreateFormListsAsync(vm);
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
                var client = CreateRegistrationServiceClient();
                var registration = await GetNullableAsync<RegistrationDto>(client, $"api/registrations/{id.Value}");

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
            if (id != vm.RegistrationId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateRegistrationEditFormListsAsync(vm);
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
                var client = CreateRegistrationServiceClient();
                await PutAsync(client, $"api/registrations/{id}", dto);

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

            await PopulateRegistrationEditFormListsAsync(vm);
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
                var client = CreateRegistrationServiceClient();
                var registration = await GetNullableAsync<RegistrationDto>(client, $"api/registrations/{id.Value}");

                if (registration == null)
                {
                    return NotFound();
                }

                var vm = MapToDeleteViewModel(registration);

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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            RegistrationDto? registration = null;

            try
            {
                var client = CreateRegistrationServiceClient();

                registration = await GetNullableAsync<RegistrationDto>(client, $"api/registrations/{id}");

                if (registration == null)
                {
                    return NotFound();
                }

                await DeleteAsync(client, $"api/registrations/{id}");

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

            if (registration == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = MapToDeleteViewModel(registration);
            return View("Delete", vm);
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync(long? selectedId = null)
        {
            var client = CreateEventServiceClient();
            var events = await GetListAsync<EventDto>(client, "api/events");

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
            var client = CreateRegistrationServiceClient();
            var participants = await GetListAsync<ParticipantDto>(client, "api/participants");

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

        private async Task PopulateRegistrationCreateFormListsAsync(RegistrationCreateViewModel vm)
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

        private async Task PopulateRegistrationEditFormListsAsync(RegistrationEditViewModel vm)
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