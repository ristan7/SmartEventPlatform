using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatformWeb.Infrastructure;
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
            try
            {
                var client = CreateClient();
                var eventTypes = await ApiHttpHelper.GetListAsync<EventTypeDto>(client, "gateway/eventtypes");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading event types. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading event types.");
            }

            return View(new List<EventTypeListViewModel>());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var eventType = await ApiHttpHelper.GetNullableAsync<EventTypeDto>(client, $"gateway/eventtypes/{id.Value}");

                if (eventType == null) return NotFound();

                var vm = new EventTypeDetailsViewModel
                {
                    EventTypeId = eventType.EventTypeId,
                    Name = eventType.Name
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading event type details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading event type details.");
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View(new EventTypeCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventTypeCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new EventTypeDto { Name = vm.Name };

            try
            {
                var client = CreateClient();
                var result = await ApiHttpHelper.PostAndReadIdAsync(client, "gateway/eventtypes", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event type could not be created.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the event type. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the event type.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var eventType = await ApiHttpHelper.GetNullableAsync<EventTypeDto>(client, $"gateway/eventtypes/{id.Value}");

                if (eventType == null) return NotFound();

                var vm = new EventTypeEditViewModel
                {
                    EventTypeId = eventType.EventTypeId,
                    Name = eventType.Name
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the event type. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the event type.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EventTypeEditViewModel vm)
        {
            if (id != vm.EventTypeId) return NotFound();
            if (!ModelState.IsValid) return View(vm);

            var dto = new EventTypeDto { EventTypeId = vm.EventTypeId, Name = vm.Name };

            try
            {
                var client = CreateClient();
                var result = await ApiHttpHelper.PutAsync(client, $"gateway/eventtypes/{id}", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event type could not be updated.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the event type. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the event type.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var eventType = await ApiHttpHelper.GetNullableAsync<EventTypeDto>(client, $"gateway/eventtypes/{id.Value}");

                if (eventType == null) return NotFound();

                return View(MapToDeleteViewModel(eventType));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the event type. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the event type.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            EventTypeDto? eventType = null;

            try
            {
                var client = CreateClient();
                eventType = await ApiHttpHelper.GetNullableAsync<EventTypeDto>(client, $"gateway/eventtypes/{id}");

                if (eventType == null) return NotFound();

                var result = await ApiHttpHelper.DeleteAsync(client, $"gateway/eventtypes/{id}");

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The event type could not be deleted.");
                    return View("Delete", MapToDeleteViewModel(eventType));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the event type. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the event type.");
            }

            if (eventType == null) return RedirectToAction(nameof(Index));

            return View("Delete", MapToDeleteViewModel(eventType));
        }

        private static EventTypeDeleteViewModel MapToDeleteViewModel(EventTypeDto eventType) =>
            new EventTypeDeleteViewModel { EventTypeId = eventType.EventTypeId, Name = eventType.Name };

        private HttpClient CreateClient() =>
            _httpClientFactory.CreateClient("ApiGateway");
    }
}