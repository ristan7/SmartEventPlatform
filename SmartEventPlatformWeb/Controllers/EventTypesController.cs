using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.EventTypes;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventTypesController : Controller
    {
        private readonly IEventApiClient _eventApiClient;

        public EventTypesController(IEventApiClient eventApiClient)
        {
            _eventApiClient = eventApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var eventTypes = await _eventApiClient.GetEventTypesAsync();

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

            var eventType = await _eventApiClient.GetEventTypeByIdAsync(id.Value);

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
                await _eventApiClient.CreateEventTypeAsync(dto);
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

            var eventType = await _eventApiClient.GetEventTypeByIdAsync(id.Value);

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
                await _eventApiClient.UpdateEventTypeAsync(id, dto);
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

            var eventType = await _eventApiClient.GetEventTypeByIdAsync(id.Value);

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
            var eventType = await _eventApiClient.GetEventTypeByIdAsync(id);

            if (eventType == null)
            {
                return NotFound();
            }

            try
            {
                await _eventApiClient.DeleteEventTypeAsync(id);
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
    }
}