using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatformWeb.ViewModels.EventTypes;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventTypesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventTypesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateEventServiceClient();
            var eventTypes = await GetListAsync<EventTypeDto>(client, "api/eventtypes");

            var vm = eventTypes
                .OrderBy(t => t.Name)
                .Select(t => new EventTypeListViewModel
                {
                    EventTypeId = t.EventTypeId,
                    Name = t.Name
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
            var eventType = await GetNullableAsync<EventTypeDto>(client, $"api/eventtypes/{id.Value}");

            if (eventType == null)
            {
                return NotFound();
            }

            var vm = new EventTypeDetailsViewModel
            {
                EventTypeId = eventType.EventTypeId,
                Name = eventType.Name
            };

            return View(vm);
        }

        public IActionResult Create()
        {
            return View(new EventTypeCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventTypeCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new EventTypeDto
            {
                Name = vm.Name
            };

            try
            {
                var client = CreateEventServiceClient();
                await PostAndReadIdAsync(client, "api/eventtypes", dto);

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

            var client = CreateEventServiceClient();
            var eventType = await GetNullableAsync<EventTypeDto>(client, $"api/eventtypes/{id.Value}");

            if (eventType == null)
            {
                return NotFound();
            }

            var vm = new EventTypeEditViewModel
            {
                EventTypeId = eventType.EventTypeId,
                Name = eventType.Name
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EventTypeEditViewModel vm)
        {
            if (id != vm.EventTypeId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new EventTypeDto
            {
                EventTypeId = vm.EventTypeId,
                Name = vm.Name
            };

            try
            {
                var client = CreateEventServiceClient();
                await PutAsync(client, $"api/eventtypes/{id}", dto);

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

            var client = CreateEventServiceClient();
            var eventType = await GetNullableAsync<EventTypeDto>(client, $"api/eventtypes/{id.Value}");

            if (eventType == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(eventType);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var client = CreateEventServiceClient();
            var eventType = await GetNullableAsync<EventTypeDto>(client, $"api/eventtypes/{id}");

            if (eventType == null)
            {
                return NotFound();
            }

            try
            {
                await DeleteAsync(client, $"api/eventtypes/{id}");

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(eventType);

                return View("Delete", vm);
            }
        }

        private static EventTypeDeleteViewModel MapToDeleteViewModel(EventTypeDto eventType)
        {
            return new EventTypeDeleteViewModel
            {
                EventTypeId = eventType.EventTypeId,
                Name = eventType.Name
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