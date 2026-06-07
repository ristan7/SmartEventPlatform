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

        public async Task<IActionResult> Create(long? eventId)
        {
            var vm = new RegistrationCreateViewModel
            {
                RegistrationDate = DateTime.Now,
                Events = await GetEventsSelectListAsync(eventId),
                Participants = await GetParticipantsSelectListAsync()
            };

            if (eventId.HasValue)
            {
                vm.EventId = eventId.Value;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistrationCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();
                return View(vm);
            }
            var alreadyRegistered = await AlreadyRegistered(vm.EventId, vm.ParticipantId);

            if (alreadyRegistered)
            {
                ModelState.AddModelError(string.Empty, "This participant is already registered for the selected event.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();

                return View(vm);
            }

            var capacityReached = await IsEventCapacityReached(vm.EventId);

            if (capacityReached)
            {
                ModelState.AddModelError(string.Empty, "Registration is not possible because the registration location capacity has been reached.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();

                return View(vm);
            }

            var registration = new Registration
            {
                EventId = vm.EventId,
                ParticipantId = vm.ParticipantId,
                RegistrationDate = vm.RegistrationDate
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .FindAsync(id);

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
                Events = await GetEventsSelectListAsync(),
                Participants = await GetParticipantsSelectListAsync()
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

            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();
                return View(vm);
            }

            var duplicateRegistration = await DuplicateRegistrationExistsAsync(
                vm.EventId,
                vm.ParticipantId,
                vm.RegistrationId);

            if (duplicateRegistration)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This participant is already registered for the selected event.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();
                return View(vm);
            }

            var capacityReached = await IsEventCapacityReached(vm.EventId, vm.RegistrationId);

            if (capacityReached)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Registration is not possible because the event location capacity has been reached.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Participants = await GetParticipantsSelectListAsync();
                return View(vm);
            }

            try
            {
                var registration = await _context.Registrations.FindAsync(id);

                if (registration == null)
                {
                    return NotFound();
                }

                registration.EventId = vm.EventId;
                registration.ParticipantId = vm.ParticipantId;
                registration.RegistrationDate = vm.RegistrationDate;

                _context.Update(registration);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RegistrationExists(vm.RegistrationId))
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
            var registration = await _context.Registrations.FindAsync(id);
            if (registration != null)
            {
                _context.Registrations.Remove(registration);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RegistrationExists(long id)
        {
            return _context.Registrations.Any(e => e.RegistrationId == id);
        }

        private Task<bool> AlreadyRegistered(long eventId, long participantId)
        {
            return _context.Registrations
                .AnyAsync(r => r.EventId == eventId && r.ParticipantId == participantId);
        }

        private async Task<bool> IsEventCapacityReached(long eventId, long? registrationIdToExclude = null)
        {
            var selectedEvent = await _context.Events
                .Include(e => e.Location)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (selectedEvent == null || selectedEvent.Location == null)
            {
                return false;
            }

            var registrationsQuery = _context.Registrations
                .Where(r => r.EventId == eventId);

            if (registrationIdToExclude.HasValue)
            {
                registrationsQuery = registrationsQuery
                    .Where(r => r.RegistrationId != registrationIdToExclude.Value);
            }

            var currentRegistrationCount = await registrationsQuery.CountAsync();

            return currentRegistrationCount >= selectedEvent.Location.Capacity;
        }

        private Task<bool> DuplicateRegistrationExistsAsync(
    long eventId,
    long participantId,
    long registrationIdToExclude)
        {
            return _context.Registrations
                .AnyAsync(r =>
                    r.RegistrationId != registrationIdToExclude &&
                    r.EventId == eventId &&
                    r.ParticipantId == participantId);
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync(long? selectedId)
        {
            return await _context.Events
                    .Select(e => new SelectListItem
                    {
                        Value = e.EventId.ToString(),
                        Text = e.EventName,
                        Selected = selectedId.HasValue && e.EventId == selectedId.Value
                    })
                    .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync()
        {
            return await _context.Events
                    .Select(e => new SelectListItem
                    {
                        Value = e.EventId.ToString(),
                        Text = e.EventName,
                    })
                    .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetParticipantsSelectListAsync()
        {
            return await _context.Participants
                    .Select(p => new SelectListItem
                    {
                        Value = p.ParticipantId.ToString(),
                        Text = p.FirstName + " " + p.LastName
                    })
                    .ToListAsync();
        }
    }
}
