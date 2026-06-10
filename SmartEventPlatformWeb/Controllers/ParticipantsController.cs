using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatformWeb.Infrastructure;
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
            try
            {
                var client = CreateRegistrationServiceClient();
                //var participants = await GetListAsync<ParticipantDto>(client, "api/participants");
                var participants = await ApiHttpHelper.GetListAsync<ParticipantDto>(client, "api/participants");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading participants. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading participants.");
            }

            return View(new List<ParticipantListViewModel>());
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
                //var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");
                var participant = await ApiHttpHelper.GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading participant details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading participant details.");
            }

            return RedirectToAction(nameof(Index));
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
                //await PostAndReadIdAsync(client, "api/participants", dto);
                var result = await ApiHttpHelper.PostAndReadIdAsync(client, "api/participants", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The participant could not be created.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the participant. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the participant.");
            }

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
                //var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");
                var participant = await ApiHttpHelper.GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the participant. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the participant.");
            }

            return RedirectToAction(nameof(Index));
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
                //await PutAsync(client, $"api/participants/{id}", dto);
                var result = await ApiHttpHelper.PutAsync(client, $"api/participants/{id}", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The participant could not be updated.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the participant. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the participant.");
            }

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
                //var participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");
                var participant = await ApiHttpHelper.GetNullableAsync<ParticipantDto>(client, $"api/participants/{id.Value}");

                if (participant == null)
                {
                    return NotFound();
                }

                var vm = MapToDeleteViewModel(participant);

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the participant. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the participant.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            ParticipantDto? participant = null;

            try
            {
                var client = CreateRegistrationServiceClient();

                //participant = await GetNullableAsync<ParticipantDto>(client, $"api/participants/{id}");
                participant = await ApiHttpHelper.GetNullableAsync<ParticipantDto>(client, $"api/participants/{id}");

                if (participant == null)
                {
                    return NotFound();
                }

                //await DeleteAsync(client, $"api/participants/{id}");
                var result = await ApiHttpHelper.DeleteAsync(client, $"api/participants/{id}");

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The participant could not be deleted.");
                    var deleteVm = MapToDeleteViewModel(participant);
                    return View("Delete", deleteVm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the participant. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the participant.");
            }

            if (participant == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = MapToDeleteViewModel(participant);
            return View("Delete", vm);
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
    }
}