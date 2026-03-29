using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.Events;

namespace SmartEventPlatformWeb.Controllers
{
    public class EventsController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public EventsController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Location)
                .OrderBy(e => e.EventDateTime)
                .Select(e => new EventListViewModel
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDateTime = e.EventDateTime,
                    LocationName = e.Location.LocationName
                })
                .ToListAsync();

            return View(events);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventDetails = await _context.Events
                .Include(e => e.Location)
                .Where(e => e.EventId == id)
                .Select(e => new EventDetailsViewModel
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Agenda = e.Agenda,
                    EventDateTime = e.EventDateTime,
                    DurationInMinutes = e.DurationInMinutes,
                    RegistrationFee = e.RegistrationFee,
                    LocationName = e.Location.LocationName,
                    LocationAddress = e.Location.Address
                })
                .FirstOrDefaultAsync();
            if (eventDetails == null)
            {
                return NotFound();
            }

            return View(eventDetails);
        }

        public IActionResult Create()
        {
            var vm = new EventCreateViewModel
            {
                Locations = _context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.LocationId.ToString(),
                        Text = l.LocationName
                    })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var @event = new Event
                {
                    EventName = vm.EventName,
                    Agenda = vm.Agenda,
                    EventDateTime = vm.EventDateTime,
                    DurationInMinutes = vm.DurationInMinutes,
                    RegistrationFee = vm.RegistrationFee,
                    LocationId = vm.LocationId
                };
                _context.Events.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            vm.Locations = _context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.LocationId.ToString(),
                    Text = l.LocationName
                })
                .ToList();
            
            return View(vm);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            var vm = new EventEditViewModel
            {
                EventId = @event.EventId,
                EventName = @event.EventName,
                Agenda = @event.Agenda,
                EventDateTime = @event.EventDateTime,
                DurationInMinutes = @event.DurationInMinutes,
                RegistrationFee = @event.RegistrationFee,
                LocationId = @event.LocationId,
                Locations = _context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.LocationId.ToString(),
                        Text = l.LocationName
                    })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EventEditViewModel vm)
        {
            if (id != vm.EventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var @event = await _context.Events.FindAsync(id);

                    if(@event == null)
                    {
                        return NotFound();
                    }
                    @event.EventName = vm.EventName;
                    @event.Agenda = vm.Agenda;
                    @event.EventDateTime = vm.EventDateTime;
                    @event.DurationInMinutes = vm.DurationInMinutes;
                    @event.RegistrationFee = vm.RegistrationFee;
                    @event.LocationId = vm.LocationId;

                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.EventId == vm.EventId))
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
            vm.Locations = _context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.LocationId.ToString(),
                        Text = l.LocationName
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

            var vm = await _context.Events
                .Include(e => e.Location)
                .Where(e => e.EventId == id)
                .Select(e => new EventDeleteViewModel
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDateTime = e.EventDateTime,
                    LocationName = e.Location.LocationName
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
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(long id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}
