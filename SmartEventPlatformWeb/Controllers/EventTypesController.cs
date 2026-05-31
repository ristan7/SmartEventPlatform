using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.EventTypes;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventTypesController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public EventTypesController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var types = await _context.EventTypes
                .OrderBy(t => t.Name)
                .Select(t => new EventTypeListViewModel
                {
                    EventTypeId = t.EventTypeId,
                    Name = t.Name
                })
                .ToListAsync();

            return View(types);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vm = await _context.EventTypes
                .Where(e => e.EventTypeId == id)
                .Select(e => new EventTypeDetailsViewModel
                {
                    EventTypeId = e.EventTypeId,
                    Name = e.Name
                })
                .FirstOrDefaultAsync();

            if (vm == null)
            {
                return NotFound();
            }

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

            var eventType = new EventType
            {
                Name = vm.Name
            };
            _context.EventTypes.Add(eventType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventType = await _context.EventTypes.FindAsync(id);
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

            try
            {
                var eventType = await _context.EventTypes.FindAsync(id);
                if (eventType == null)
                {
                    return NotFound();
                }

                eventType.Name = vm.Name;

                _context.Update(eventType);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventTypeExists(vm.EventTypeId))
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

            var vm = await _context.EventTypes
                .Where(e => e.EventTypeId == id)
                .Select(e => new EventTypeDeleteViewModel
                {
                    EventTypeId = e.EventTypeId,
                    Name = e.Name
                })
                .FirstOrDefaultAsync();

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
            var eventType = await _context.EventTypes.FindAsync(id);

            if (eventType == null)
            {
                return NotFound();
            }
            try
            {
                _context.EventTypes.Remove(eventType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
        "This eventType type cannot be deleted because it is used by one or more events.");

                var vm = new EventTypeDeleteViewModel
                {
                    EventTypeId = eventType.EventTypeId,
                    Name = eventType.Name
                };

                return View("Delete", vm);
            }
        }

        private bool EventTypeExists(long id)
        {
            return _context.EventTypes.Any(e => e.EventTypeId == id);
        }
    }
}
