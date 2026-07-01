using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatformWeb.Infrastructure;
using SmartEventPlatformWeb.ViewModels.Locations;
using System.Net;
using System.Net.Http.Json;

namespace SmartEventPlatformWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LocationsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = CreateClient();
                var locations = await ApiHttpHelper.GetListAsync<LocationDto>(client, "gateway/locations");

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
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading locations. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading locations.");
            }

            return View(new List<LocationListViewModel>());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var location = await ApiHttpHelper.GetNullableAsync<LocationDto>(client, $"gateway/locations/{id.Value}");

                if (location == null) return NotFound();

                var vm = new LocationDetailsViewModel
                {
                    LocationId = location.LocationId,
                    LocationName = location.LocationName,
                    Address = location.Address,
                    Capacity = location.Capacity
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading location details. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading location details.");
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View(new LocationCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocationCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new LocationDto
            {
                LocationName = vm.LocationName,
                Address = vm.Address,
                Capacity = vm.Capacity
            };

            try
            {
                var client = CreateClient();
                var result = await ApiHttpHelper.PostAndReadIdAsync(client, "gateway/locations", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The location could not be created.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while creating the location. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the location.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var location = await ApiHttpHelper.GetNullableAsync<LocationDto>(client, $"gateway/locations/{id.Value}");

                if (location == null) return NotFound();

                var vm = new LocationEditViewModel
                {
                    LocationId = location.LocationId,
                    LocationName = location.LocationName,
                    Address = location.Address,
                    Capacity = location.Capacity
                };

                return View(vm);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the location. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the location.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, LocationEditViewModel vm)
        {
            if (id != vm.LocationId) return NotFound();
            if (!ModelState.IsValid) return View(vm);

            var dto = new LocationDto
            {
                LocationId = vm.LocationId,
                LocationName = vm.LocationName,
                Address = vm.Address,
                Capacity = vm.Capacity
            };

            try
            {
                var client = CreateClient();
                var result = await ApiHttpHelper.PutAsync(client, $"gateway/locations/{id}", dto);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The location could not be updated.");
                    return View(vm);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while updating the location. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the location.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var client = CreateClient();
                var location = await ApiHttpHelper.GetNullableAsync<LocationDto>(client, $"gateway/locations/{id.Value}");

                if (location == null) return NotFound();

                return View(MapToDeleteViewModel(location));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while loading the location. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while loading the location.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            LocationDto? location = null;

            try
            {
                var client = CreateClient();
                location = await ApiHttpHelper.GetNullableAsync<LocationDto>(client, $"gateway/locations/{id}");

                if (location == null) return NotFound();

                var result = await ApiHttpHelper.DeleteAsync(client, $"gateway/locations/{id}");

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The location could not be deleted.");
                    return View("Delete", MapToDeleteViewModel(location));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Request timeout expired while deleting the location. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the location.");
            }

            if (location == null) return RedirectToAction(nameof(Index));

            return View("Delete", MapToDeleteViewModel(location));
        }

        private static LocationDeleteViewModel MapToDeleteViewModel(LocationDto location) =>
            new LocationDeleteViewModel
            {
                LocationId = location.LocationId,
                LocationName = location.LocationName,
                Address = location.Address,
                Capacity = location.Capacity
            };

        private HttpClient CreateClient() =>
            _httpClientFactory.CreateClient("ApiGateway");
    }
}