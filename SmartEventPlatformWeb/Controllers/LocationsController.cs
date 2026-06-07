using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatformWeb.Services;
using SmartEventPlatformWeb.ViewModels.Locations;

namespace SmartEventPlatformWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IEventApiClient _eventApiClient;

        public LocationsController(IEventApiClient eventApiClient)
        {
            _eventApiClient = eventApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var locations = await _eventApiClient.GetLocationsAsync();

            var vm = locations
                .OrderBy(l => l.LocationName)
                .Select(l => new LocationListViewModel
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
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

            var location = await _eventApiClient.GetLocationByIdAsync(id.Value);

            if (location == null)
            {
                return NotFound();
            }

            var vm = new LocationDetailsViewModel
            {
                LocationId = location.LocationId,
                LocationName = location.LocationName,
                Address = location.Address,
                Capacity = location.Capacity
            };

            return View(vm);
        }

        public IActionResult Create()
        {
            return View(new LocationCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocationCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new LocationDto
            {
                LocationName = vm.LocationName,
                Address = vm.Address,
                Capacity = vm.Capacity
            };

            try
            {
                await _eventApiClient.CreateLocationAsync(dto);
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

            var location = await _eventApiClient.GetLocationByIdAsync(id.Value);

            if (location == null)
            {
                return NotFound();
            }

            var vm = new LocationEditViewModel
            {
                LocationId = location.LocationId,
                LocationName = location.LocationName,
                Address = location.Address,
                Capacity = location.Capacity
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, LocationEditViewModel vm)
        {
            if (id != vm.LocationId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = new LocationDto
            {
                LocationId = vm.LocationId,
                LocationName = vm.LocationName,
                Address = vm.Address,
                Capacity = vm.Capacity
            };

            try
            {
                await _eventApiClient.UpdateLocationAsync(id, dto);
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

            var location = await _eventApiClient.GetLocationByIdAsync(id.Value);

            if (location == null)
            {
                return NotFound();
            }

            var vm = MapToDeleteViewModel(location);

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var location = await _eventApiClient.GetLocationByIdAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            try
            {
                await _eventApiClient.DeleteLocationAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var vm = MapToDeleteViewModel(location);

                return View("Delete", vm);
            }
        }

        private static LocationDeleteViewModel MapToDeleteViewModel(LocationDto location)
        {
            return new LocationDeleteViewModel
            {
                LocationId = location.LocationId,
                LocationName = location.LocationName,
                Address = location.Address,
                Capacity = location.Capacity
            };
        }
    }
}