using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatformWeb.Infrastructure;
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
            try
            {
                var client = CreateClient();
                var speakers = await ApiHttpHelper.GetListAsync<SpeakerDto>(client, "gateway/speakers");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading speakers. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading speakers.");
            }

            return View(new List<SpeakerListViewModel>());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var speaker = await ApiHttpHelper.GetNullableAsync<SpeakerDto>(client, $"gateway/speakers/{id.Value}");

                if (speaker == null) return NotFound();

                var eventSpeakers = await ApiHttpHelper.GetListAsync<EventSpeakerDto>(client, $"gateway/eventspeakers/by-speaker/{id.Value}");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading speaker details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading speaker details.");
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View(new SpeakerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpeakerCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new SpeakerDto
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Title = vm.Title,
                ExpertiseAreas = vm.ExpertiseAreas
            };

            try
            {
                var client = CreateClient();
                var result = await ApiHttpHelper.PostAndReadIdAsync(client, "gateway/speakers", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The speaker could not be created.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the speaker. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the speaker.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var speaker = await ApiHttpHelper.GetNullableAsync<SpeakerDto>(client, $"gateway/speakers/{id.Value}");

                if (speaker == null) return NotFound();

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the speaker. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the speaker.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, SpeakerEditViewModel vm)
        {
            if (id != vm.SpeakerId) return NotFound();
            if (!ModelState.IsValid) return View(vm);

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
                var client = CreateClient();
                var result = await ApiHttpHelper.PutAsync(client, $"gateway/speakers/{id}", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The speaker could not be updated.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the speaker. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the speaker.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var speaker = await ApiHttpHelper.GetNullableAsync<SpeakerDto>(client, $"gateway/speakers/{id.Value}");

                if (speaker == null) return NotFound();

                return View(MapToDeleteViewModel(speaker));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the speaker. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the speaker.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            SpeakerDto? speaker = null;

            try
            {
                var client = CreateClient();
                speaker = await ApiHttpHelper.GetNullableAsync<SpeakerDto>(client, $"gateway/speakers/{id}");

                if (speaker == null) return NotFound();

                var result = await ApiHttpHelper.DeleteAsync(client, $"gateway/speakers/{id}");

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The speaker could not be deleted.");
                    return View("Delete", MapToDeleteViewModel(speaker));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the speaker. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the speaker.");
            }

            if (speaker == null) return RedirectToAction(nameof(Index));

            return View("Delete", MapToDeleteViewModel(speaker));
        }

        private static SpeakerDeleteViewModel MapToDeleteViewModel(SpeakerDto speaker) =>
            new SpeakerDeleteViewModel
            {
                SpeakerId = speaker.SpeakerId,
                FirstName = speaker.FirstName,
                LastName = speaker.LastName,
                Title = speaker.Title,
                ExpertiseAreas = speaker.ExpertiseAreas
            };

        private HttpClient CreateClient() =>
            _httpClientFactory.CreateClient("ApiGateway");
    }
}