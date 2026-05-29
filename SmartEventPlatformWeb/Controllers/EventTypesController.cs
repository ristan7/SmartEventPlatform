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
            if (ModelState.IsValid)
            {
                var @event = new EventType
                {
                    Name = vm.Name
                };
                _context.EventTypes.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.EventTypes.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            var vm = new EventTypeEditViewModel
            {
                EventTypeId = @event.EventTypeId,
                Name = @event.Name
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

            if (ModelState.IsValid)
            {
                try
                {
                    var @event = await _context.EventTypes.FindAsync(id);
                    if (@event == null)
                    {
                        return NotFound();
                    }

                    @event.Name = vm.Name;

                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.EventTypes.Any(e => e.EventTypeId == vm.EventTypeId))
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
            return View(vm);
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
            var @event = await _context.EventTypes.FindAsync(id);

            if (@event == null)
            {
                return NotFound();
            }

            try
            {
                _context.EventTypes.Remove(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
        "This event type cannot be deleted because it is used by one or more events.");

                var vm = new EventTypeDeleteViewModel
                {
                    EventTypeId = @event.EventTypeId,
                    Name = @event.Name
                };

                return View("Delete", vm);
            }


        }
    }
}
