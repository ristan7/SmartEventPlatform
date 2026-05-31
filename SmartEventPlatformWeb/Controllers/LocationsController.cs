using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.Locations;

namespace SmartEventPlatformWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public LocationsController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var locations = await _context.Locations
                .OrderBy(l => l.LocationName)
                .Select(l => new LocationListViewModel
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
                }).ToListAsync();

            return View(locations);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vm = await _context.Locations
                .Where(l => l.LocationId == id)
                .Select(l => new LocationDetailsViewModel
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
                }).FirstOrDefaultAsync();

            if (vm == null)
            {
                return NotFound();
            }

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
            var location = new Location
            {
                LocationName = vm.LocationName,
                Address = vm.Address,
                Capacity = vm.Capacity
            };
            _context.Add(location);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations.FindAsync(id);
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
            try
            {
                var location = await _context.Locations.FindAsync(id);
                if (location == null)
                {
                    return NotFound();
                }

                location.LocationName = vm.LocationName;
                location.Address = vm.Address;
                location.Capacity = vm.Capacity;

                _context.Update(location);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocationExists(vm.LocationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vm = await _context.Locations
                .Where(l => l.LocationId == id)
                .Select(l => new LocationDeleteViewModel
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
                }).FirstOrDefaultAsync();

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            if (await LocationHasDependenciesAsync(id))
            {
                return ReturnLocationDeleteViewWithError(
                    location,
                    "This location cannot be deleted because it is used by one or more events.");
            }

            try
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                return ReturnLocationDeleteViewWithError(
                    location,
                    "This location cannot be deleted because it is used by one or more events.");
            }
        }

        private bool LocationExists(long id)
        {
            return _context.Locations.Any(l => l.LocationId == id);
        }

        private async Task<bool> LocationHasDependenciesAsync(long locationId)
        {
            return await _context.Events
                .AnyAsync(e => e.LocationId == locationId);
        }

        private static LocationDeleteViewModel MapToDeleteViewModel(Location location)
        {
            return new LocationDeleteViewModel
            {
                LocationId = location.LocationId,
                LocationName = location.LocationName,
                Address = location.Address,
                Capacity = location.Capacity
            };
        }

        private IActionResult ReturnLocationDeleteViewWithError(Location location, string errorMessage)
        {
            ModelState.AddModelError(string.Empty, errorMessage);

            var vm = MapToDeleteViewModel(location);

            return View("Delete", vm);
        }
    }
}
