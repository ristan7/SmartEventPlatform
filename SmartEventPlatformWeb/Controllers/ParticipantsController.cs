using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.Participants;

namespace SmartEventPlatformWeb.Controllers
{
    public class ParticipantsController : Controller
    {
        private readonly IRegistrationApiClient _registrationApiClient;

        public ParticipantsController(IRegistrationApiClient registrationApiClient)
        {
            _registrationApiClient = registrationApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var participants = await _registrationApiClient.GetParticipantsAsync();

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

            var participant = await _registrationApiClient.GetParticipantByIdAsync(id.Value);

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
                await _registrationApiClient.CreateParticipantAsync(dto);
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

            var participant = await _registrationApiClient.GetParticipantByIdAsync(id.Value);

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
                await _registrationApiClient.UpdateParticipantAsync(id, dto);
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

            var participant = await _registrationApiClient.GetParticipantByIdAsync(id.Value);

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
            var participant = await _registrationApiClient.GetParticipantByIdAsync(id);

            if (participant == null)
            {
                return NotFound();
            }

            try
            {
                await _registrationApiClient.DeleteParticipantAsync(id);
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
    }
}