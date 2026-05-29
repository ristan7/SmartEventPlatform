using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.EventSpeakers;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventSpeakersController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public EventSpeakersController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var eventSpeakers = await _context.EventSpeakers
                .Include(es => es.Event)
                .Include(es => es.Speaker)
                .OrderBy(es => es.Event!.EventName)
                .ThenBy(es => es.Time)
                .Select(es => new EventSpeakerListViewModel
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerName = es.Speaker != null ? es.Speaker.FirstName + " " + es.Speaker.LastName : string.Empty,
                    Topic = es.Topic,
                    Time = es.Time,
                    EventId = es.EventId,
                    SpeakerId = es.SpeakerId
                }).ToListAsync();

            return View(eventSpeakers);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var vm = await _context.EventSpeakers
                .Include(es => es.Event)
                .Include(es => es.Speaker)
                .Where(es => es.EventSpeakerId == id)
                .Select(es => new EventSpeakerDetailsViewModel
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerFullName = es.Speaker != null ? es.Speaker.FirstName + " " + es.Speaker.LastName : string.Empty,
                    Topic = es.Topic,
                    Time = es.Time,
                    EventId = es.EventId,
                    SpeakerId = es.SpeakerId
                }).FirstOrDefaultAsync();

            if (vm == null) return NotFound();

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new EventSpeakerCreateViewModel
            {
                Time = DateTime.Now,
                Events = await GetEventsSelectListAsync(),
                Speakers = await GetSpeakersSelectListAsync(),
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventSpeakerCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
                return View(vm);
            }

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(vm.EventId, vm.Time);

            if (!isTimeValid)
            {
                ModelState.AddModelError(nameof(vm.Time),
                    "Speaker time must be within the selected event duration.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();

                return View(vm);
            }

            var eventSpeaker = new EventSpeaker
            {
                EventId = vm.EventId,
                SpeakerId = vm.SpeakerId,
                Topic = vm.Topic,
                Time = vm.Time
            };

            _context.EventSpeakers.Add(eventSpeaker);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var eventSpeaker = await _context.EventSpeakers.FindAsync(id);
            if (eventSpeaker == null) return NotFound();

            var vm = new EventSpeakerEditViewModel
            {
                EventSpeakerId = eventSpeaker.EventSpeakerId,
                EventId = eventSpeaker.EventId,
                SpeakerId = eventSpeaker.SpeakerId,
                Topic = eventSpeaker.Topic,
                Time = eventSpeaker.Time,
                Events = await GetEventsSelectListAsync(),
                Speakers = await GetSpeakersSelectListAsync(),
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EventSpeakerEditViewModel vm)
        {
            if (id != vm.EventSpeakerId) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();
                return View(vm);
            }

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(vm.EventId, vm.Time);

            if (!isTimeValid)
            {
                ModelState.AddModelError(nameof(vm.Time),
                    "Speaker time must be within the selected event duration.");

                vm.Events = await GetEventsSelectListAsync();
                vm.Speakers = await GetSpeakersSelectListAsync();

                return View(vm);
            }

            var eventSpeaker = await _context.EventSpeakers.FirstOrDefaultAsync(es => es.EventSpeakerId == id);

            if (eventSpeaker == null) return NotFound();

            eventSpeaker.EventId = vm.EventId;
            eventSpeaker.SpeakerId = vm.SpeakerId;
            eventSpeaker.Topic = vm.Topic;
            eventSpeaker.Time = vm.Time;

            try
            {
                _context.EventSpeakers.Update(eventSpeaker);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventSpeakerExists(vm.EventSpeakerId)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var vm = await _context.EventSpeakers
                .Include(es => es.Event)
                .Include(es => es.Speaker)
                .Where(es => es.EventSpeakerId == id)
                .Select(es => new EventSpeakerDeleteViewModel
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerFullName = es.Speaker != null ? es.Speaker.FirstName + " " + es.Speaker.LastName : string.Empty,
                    Topic = es.Topic,
                    Time = es.Time
                }).FirstOrDefaultAsync();

            if (vm == null) return NotFound();

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var eventSpeaker = await _context.EventSpeakers.FirstOrDefaultAsync(eventSpeaker => eventSpeaker.EventSpeakerId == id);

            if (eventSpeaker != null)
            {
                _context.EventSpeakers.Remove(eventSpeaker);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EventSpeakerExists(long id)
        {
            return _context.EventSpeakers.Any(e => e.EventSpeakerId == id);
        }

        private async Task<List<SelectListItem>> GetEventsSelectListAsync()
        {
            return await _context.Events
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventName
                }).ToListAsync();
        }

        private async Task<List<SelectListItem>> GetSpeakersSelectListAsync()
        {
            return await _context.Speakers
                .Select(s => new SelectListItem
                {
                    Value = s.SpeakerId.ToString(),
                    Text = s.FirstName + " " + s.LastName
                }).ToListAsync();
        }

        private async Task<bool> IsSpeakerTimeInsideEventAsync(long eventId, DateTime speakerTime)
        {
            var selectedEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (selectedEvent == null)
            {
                return false;
            }

            var eventStart = selectedEvent.EventDateTime;
            var eventEnd = selectedEvent.EventDateTime.AddMinutes(selectedEvent.DurationInMinutes);

            return speakerTime >= eventStart && speakerTime <= eventEnd;
        }

    }
}
