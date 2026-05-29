using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.Registrations;

namespace SmartEventPlatformWeb.Controllers
{
    public class RegistrationsController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public RegistrationsController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var regs = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Participant)
                .OrderBy(r => r.RegistrationDate)
                .Select(r => new RegistrationListViewModel
                {
                    RegistrationId = r.RegistrationId,
                    EventName = r.Event != null ? r.Event.EventName : string.Empty,
                    ParticipantFullName = r.Participant != null ? r.Participant.FirstName + " " + r.Participant.LastName : string.Empty,
                    RegistrationDate = r.RegistrationDate
                })
                .ToListAsync();

            return View(regs);
        }

        public IActionResult Create()
        {
            var vm = new RegistrationCreateViewModel
            {
                Events = _context.Events
                    .Select(e => new SelectListItem
                    {
                        Value = e.EventId.ToString(),
                        Text = e.EventName
                    })
                    .ToList(),
                Participants = _context.Participants
                    .Select(p => new SelectListItem
                    {
                        Value = p.ParticipantId.ToString(),
                        Text = p.FirstName + " " + p.LastName
                    })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistrationCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var @event = new Registration
                {
                    EventId = vm.EventId,
                    ParticipantId = vm.ParticipantId,
                    RegistrationDate = vm.RegistrationDate
                };
                _context.Registrations.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            vm.Events = _context.Events
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventName
                })
                .ToList();
            vm.Participants = _context.Participants
                .Select(p => new SelectListItem
                {
                    Value = p.ParticipantId.ToString(),
                    Text = p.FirstName + " " + p.LastName
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

            var vm = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Participant)
                .Where(r => r.RegistrationId == id)
                .Select(r => new RegistrationDetailsViewModel
                {
                    RegistrationId = r.RegistrationId,
                    EventName = r.Event != null ? r.Event.EventName : string.Empty,
                    ParticipantFullName = r.Participant != null
                        ? r.Participant.FirstName + " " + r.Participant.LastName
                        : string.Empty,
                    RegistrationDate = r.RegistrationDate
                })
                .FirstOrDefaultAsync();

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
            {
                return NotFound();
            }

            var vm = new RegistrationEditViewModel
            {
                RegistrationId = registration.RegistrationId,
                EventId = registration.EventId,
                ParticipantId = registration.ParticipantId,
                RegistrationDate = registration.RegistrationDate,
                Events = _context.Events
                    .Select(e => new SelectListItem
                    {
                        Value = e.EventId.ToString(),
                        Text = e.EventName
                    })
                    .ToList(),
                Participants = _context.Participants
                    .Select(p => new SelectListItem
                    {
                        Value = p.ParticipantId.ToString(),
                        Text = p.FirstName + " " + p.LastName
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, RegistrationEditViewModel vm)
        {
            if (id != vm.RegistrationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var registration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.RegistrationId == id);

                if (registration == null)
                {
                    return NotFound();
                }

                registration.EventId = vm.EventId;
                registration.ParticipantId = vm.ParticipantId;
                registration.RegistrationDate = vm.RegistrationDate;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            vm.Events = _context.Events
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventName
                })
                .ToList();

            vm.Participants = _context.Participants
                .Select(p => new SelectListItem
                {
                    Value = p.ParticipantId.ToString(),
                    Text = p.FirstName + " " + p.LastName
                })
                .ToList();

            return View(vm);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vm = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Participant)
                .Where(r => r.RegistrationId == id)
                .Select(r => new RegistrationDeleteViewModel
                {
                    RegistrationId = r.RegistrationId,
                    EventName = r.Event != null ? r.Event.EventName : string.Empty,
                    ParticipantFullName = r.Participant != null ? r.Participant.FirstName + " " + r.Participant.LastName : string.Empty,
                    RegistrationDate = r.RegistrationDate
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
            var @event = await _context.Registrations.FindAsync(id);
            if (@event != null)
            {
                _context.Registrations.Remove(@event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
