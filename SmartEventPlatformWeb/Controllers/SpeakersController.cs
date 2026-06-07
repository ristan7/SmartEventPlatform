using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.Speakers;

namespace SmartEventPlatformWeb.Controllers
{
    public class SpeakersController : Controller
    {
        private readonly IEventApiClient _eventApiClient;

        public SpeakersController(IEventApiClient eventApiClient)
        {
            _eventApiClient = eventApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var speakers = await _eventApiClient.GetSpeakersAsync();

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

            var speaker = await _eventApiClient.GetSpeakerByIdAsync(id.Value);

            if (speaker == null)
            {
                return NotFound();
            }

            var eventSpeakers = await _eventApiClient.GetEventSpeakersAsync();

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
                await _eventApiClient.CreateSpeakerAsync(dto);
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

            var speaker = await _eventApiClient.GetSpeakerByIdAsync(id.Value);

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
                await _eventApiClient.UpdateSpeakerAsync(id, dto);
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

            var speaker = await _eventApiClient.GetSpeakerByIdAsync(id.Value);

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
            var speaker = await _eventApiClient.GetSpeakerByIdAsync(id);

            if (speaker == null)
            {
                return NotFound();
            }

            try
            {
                await _eventApiClient.DeleteSpeakerAsync(id);
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
    }
}