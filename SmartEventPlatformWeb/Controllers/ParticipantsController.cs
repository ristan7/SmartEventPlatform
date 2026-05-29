using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.Participants;

namespace SmartEventPlatformWeb.Controllers
{
    public class ParticipantsController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public ParticipantsController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var part = await _context.Participants
                .OrderBy(p => p.LastName)
                .Select(p => new ParticipantListViewModel
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
                }).ToListAsync();
            return View(part);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vm = await _context.Participants
                .Where(p => p.ParticipantId == id)
                .Select(p => new ParticipantDetailsViewModel
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
                })
                .FirstOrDefaultAsync();

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }

        public IActionResult Create() => View(new ParticipantCreateViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParticipantCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var participant = new Participant
                {
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email
                };
                _context.Participants.Add(participant);
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

            var @event = await _context.Participants.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            var vm = new ParticipantEditViewModel
            {
                ParticipantId = @event.ParticipantId,
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                Email = @event.Email
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

            if (ModelState.IsValid)
            {
                try
                {
                    var @event = await _context.Participants.FindAsync(id);
                    if (@event == null)
                    {
                        return NotFound();
                    }

                    @event.FirstName = vm.FirstName;
                    @event.LastName = vm.LastName;
                    @event.Email = vm.Email;
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Participants.Any(p => p.ParticipantId == vm.ParticipantId))
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

            var vm = await _context.Participants
                .Where(p => p.ParticipantId == id)
                .Select(p => new ParticipantDeleteViewModel
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
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
            var @event = await _context.Participants.FindAsync(id);
            if (@event != null)
            {
                _context.Participants.Remove(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}