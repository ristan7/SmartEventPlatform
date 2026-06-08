using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatformWeb.ViewModels.Speakers;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class SpeakersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SpeakersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateEventServiceClient();
            var speakers = await GetListAsync<SpeakerDto>(client, "api/speakers");

            var vm = speakers
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new SpeakerListViewModel
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas
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

            var speaker = await GetNullableAsync<SpeakerDto>(client, $"api/speakers/{id.Value}");

            if (speaker == null)
            {
                return NotFound();
            }

            var eventSpeakers = await GetListAsync<EventSpeakerDto>(client, "api/eventspeakers");

            var vm = new SpeakerDetailsViewModel
            {
                SpeakerId = speaker.SpeakerId,
                FirstName = speaker.FirstName,
                LastName = speaker.LastName,
                Title = speaker.Title,
                ExpertiseAreas = speaker.ExpertiseAreas,
                EventSpeakersParticipations = eventSpeakers
                    .Where(es => es.SpeakerId == speaker.SpeakerId)
                    .OrderBy(es => es.Time)
                    .Select(es => new SpeakerEventItemViewModel
                    {
                        EventSpeakerId = es.EventSpeakerId,
                        EventId = es.EventId,
                        EventName = es.EventName,
                        Topic = es.Topic,
                        Time = es.Time
                    })
                    .ToList()
            };

            return View(vm);
        }

        public IActionResult Create()
        {
            return View(new SpeakerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpeakerCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new SpeakerDto
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Title = vm.Title,
                ExpertiseAreas = vm.ExpertiseAreas
            };

            try
            {
                var client = CreateEventServiceClient();
                await PostAndReadIdAsync(client, "api/speakers", dto);

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
            var speaker = await GetNullableAsync<SpeakerDto>(client, $"api/speakers/{id.Value}");

            if (speaker == null)
            {
                return NotFound();
            }

            var vm = new SpeakerEditViewModel
            {
                SpeakerId = speaker.SpeakerId,
                FirstName = speaker.FirstName,
                LastName = speaker.LastName,
                Title = speaker.Title,
                ExpertiseAreas = speaker.ExpertiseAreas
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, SpeakerEditViewModel vm)
        {
            if (id != vm.SpeakerId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new SpeakerDto
            {
                SpeakerId = vm.SpeakerId,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Title = vm.Title,
                ExpertiseAreas = vm.ExpertiseAreas
            };

            try
            {
                var client = CreateEventServiceClient();
                await PutAsync(client, $"api/speakers/{id}", dto);

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
            var speaker = await GetNullableAsync<SpeakerDto>(client, $"api/speakers/{id.Value}");

            if (speaker == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(speaker);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var client = CreateEventServiceClient();
            var speaker = await GetNullableAsync<SpeakerDto>(client, $"api/speakers/{id}");

            if (speaker == null)
            {
                return NotFound();
            }

            try
            {
                await DeleteAsync(client, $"api/speakers/{id}");

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(speaker);

                return View("Delete", vm);
            }
        }

        private static SpeakerDeleteViewModel MapToDeleteViewModel(SpeakerDto speaker)
        {
            return new SpeakerDeleteViewModel
            {
                SpeakerId = speaker.SpeakerId,
                FirstName = speaker.FirstName,
                LastName = speaker.LastName,
                Title = speaker.Title,
                ExpertiseAreas = speaker.ExpertiseAreas
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