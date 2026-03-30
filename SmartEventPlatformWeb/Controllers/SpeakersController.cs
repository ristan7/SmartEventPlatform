using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatformWeb.Data;
using SmartEventPlatformWeb.Domains;
using SmartEventPlatformWeb.ViewModels.Speakers;

namespace SmartEventPlatformWeb.Controllers
{
    public class SpeakersController : Controller
    {
        private readonly SmartPlatformDbContext _context;

        public SpeakersController(SmartPlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var speakers = await _context.Speakers
                .OrderBy(s => s.LastName)
                .Select(s => new SpeakerListViewModel
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas
                }).ToListAsync();

            return View(speakers);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speakerDetails = await _context.Speakers
                .Include(s => s.EventSpeakers)
                .ThenInclude(es => es.Event)
                .Include(s => s.EventSpeakers)
                .ThenInclude(es => es.EventRole)
                .Where(s => s.SpeakerId == id)
                .Select(s => new SpeakerDetailsViewModel
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas,
                    EventSpeakersParticipations = s.EventSpeakers
                    .OrderBy(es => es.Time)
                    .Select(es => new SpeakerEventItemViewModel
                    {
                        EventSpeakerId = es.EventSpeakerId,
                        EventId = es.EventId,
                        EventName = es.Event!.EventName,
                        RoleName = es.EventRole!.Name,
                        Topic = es.Topic,
                        Time = es.Time
                    }).ToList()
                }).FirstOrDefaultAsync();
            
            if (speakerDetails == null)
            {
                return NotFound();
            }

            return View(speakerDetails);
        }

        public IActionResult Create()
        {
            return View(new SpeakerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpeakerCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {   
                var speaker = new Speaker
                {
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Title = vm.Title,
                    ExpertiseAreas = vm.ExpertiseAreas
                };
                _context.Add(speaker);
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

            var speaker = await _context.Speakers.FindAsync(id);
            if (speaker == null)
            {
                return NotFound();
            }
            var vm = new SpeakerEditViewModel
            {
                SpeakerId = speaker.SpeakerId,
                FirstName = speaker.FirstName,
                LastName = speaker.LastName,
                Title = speaker.Title,
                ExpertiseAreas = speaker.ExpertiseAreas
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, SpeakerEditViewModel vm)
        {
            if (id != vm.SpeakerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var speaker = await _context.Speakers.FindAsync(id);
                    if(speaker == null)
                    {
                        return NotFound();
                    }

                    speaker.FirstName = vm.FirstName;
                    speaker.LastName = vm.LastName;
                    speaker.Title = vm.Title;
                    speaker.ExpertiseAreas = vm.ExpertiseAreas;

                    _context.Update(speaker);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpeakerExists(vm.SpeakerId))
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

            var vm = await _context.Speakers
                .Where(s => s.SpeakerId == id)
                .Select(s => new SpeakerDeleteViewModel
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas
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
            var speaker = await _context.Speakers.FindAsync(id);
            if (speaker != null)
            {
                _context.Speakers.Remove(speaker);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool SpeakerExists(long id)
        {
            return _context.Speakers.Any(s => s.SpeakerId == id);
        }
    }
}
