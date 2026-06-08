using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatformWeb.ViewModels.Participants;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class ParticipantsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ParticipantsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateRegistrationServiceClient();
            var participants = await GetListAsync<ParticipantDto>(client, "api/participants");

            var vm = participants
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new ParticipantListViewModel
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
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
            var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");

            if (participant == null)
            {
                return NotFound();
            }

            var vm = new ParticipantDetailsViewModel
            {
                ParticipantId = participant.ParticipantId,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                Email = participant.Email
            };

            return View(vm);
        }

        public IActionResult Create()
        {
            return View(new ParticipantCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParticipantCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new ParticipantDto
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email
            };

            try
            {
                var client = CreateRegistrationServiceClient();
                await PostAndReadIdAsync(client, "api/participants", dto);

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
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
            var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");

            if (participant == null)
            {
                return NotFound();
            }

            var vm = new ParticipantEditViewModel
            {
                ParticipantId = participant.ParticipantId,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                Email = participant.Email
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, ParticipantEditViewModel vm)
        {
            if (id != vm.ParticipantId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new ParticipantDto
            {
                ParticipantId = vm.ParticipantId,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email
            };

            try
            {
                var client = CreateRegistrationServiceClient();
                await PutAsync(client, $"api/participants/{id}", dto);

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
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
            var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");

            if (participant == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(participant);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var client = CreateRegistrationServiceClient();
            var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id}");

            if (participant == null)
            {
                return NotFound();
            }

            try
            {
                await DeleteAsync(client, $"api/participants/{id}");

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(participant);

                return View("Delete", vm);
            }
        }

        private static ParticipantDeleteViewModel MapToDeleteViewModel(ParticipantDto participant)
        {
            return new ParticipantDeleteViewModel
            {
                ParticipantId = participant.ParticipantId,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                Email = participant.Email
            };
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