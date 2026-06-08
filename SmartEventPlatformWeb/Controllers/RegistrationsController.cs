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

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                var client = CreateRegistrationServiceClient();
                await PostAndReadIdAsync(client, "api/registrations", dto);

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

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                var client = CreateRegistrationServiceClient();
                await PutAsync(client, $"api/registrations/{id}", dto);

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

            var client = CreateRegistrationServiceClient();
            var registration = await GetNullableAsync<RegistrationDto>(client, $"api/registrations/{id.Value}");

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
            var client = CreateRegistrationServiceClient();
            var registration = await GetNullableAsync<RegistrationDto>(client, $"api/registrations/{id}");

            if (registration == null)
            {
                return NotFound();
            }

            try
            {
                await DeleteAsync(client, $"api/registrations/{id}");

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