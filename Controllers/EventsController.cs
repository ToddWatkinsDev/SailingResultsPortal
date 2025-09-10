using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Helpers;
using SailingResultsPortal.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SailingResultsPortal.Controllers
{
    [Authorize(Roles = "Sudo,Organiser")]
    public class EventsController : Controller
    {
        private const string EventsFile = "events.json";

        // GET: /Events
        public async Task<IActionResult> Index()
        {
            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            return View(events);
        }

        // GET: /Events/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event newEvent)
        {
            if (!ModelState.IsValid)
                return View(newEvent);

            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            newEvent.Id = Guid.NewGuid().ToString();
            newEvent.Races = new();

            events.Add(newEvent);
            await FileStorageHelper.SaveAsync(EventsFile, events);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound();

            return View(ev);
        }

        // POST: /Events/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Event updatedEvent)
        {
            if (!ModelState.IsValid)
                return View(updatedEvent);

            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound();

            ev.Name = updatedEvent.Name;
            await FileStorageHelper.SaveAsync(EventsFile, events);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Races?eventId=...
        public async Task<IActionResult> Races(string eventId)
        {
            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            return View(ev);
        }

        // GET: /Events/Races/Create?eventId=...
        public IActionResult CreateRace(string eventId)
        {
            ViewData["EventId"] = eventId;
            return View(new Race { HandicapType = "Open", HandicapSystem = "Portsmouth" });
        }

        // POST: /Events/Races/Create?eventId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRace(string eventId, Race newRace)
        {
            if (!ModelState.IsValid)
            {
                ViewData["EventId"] = eventId;
                return View(newRace);
            }

            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            newRace.Id = Guid.NewGuid().ToString();
            newRace.Classes = new();

            ev.Races.Add(newRace);
            await FileStorageHelper.SaveAsync(EventsFile, events);

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // GET: /Events/Races/Edit?eventId=...&raceId=...
        public async Task<IActionResult> EditRace(string eventId, string raceId)
        {
            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            var race = ev?.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            ViewData["EventId"] = eventId;
            return View(race);
        }

        // POST: /Events/Races/Edit?eventId=...&raceId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRace(string eventId, string raceId, Race updatedRace)
        {
            if (!ModelState.IsValid)
            {
                ViewData["EventId"] = eventId;
                return View(updatedRace);
            }

            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            var race = ev?.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            race.Name = updatedRace.Name;
            race.HandicapType = updatedRace.HandicapType;
            race.HandicapSystem = updatedRace.HandicapSystem;

            await FileStorageHelper.SaveAsync(EventsFile, events);

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // GET: /Events/Classes?eventId=...&raceId=...
        public async Task<IActionResult> Classes(string eventId, string raceId)
        {
            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            var race = ev?.Races.FirstOrDefault(r => r.Id == raceId);
            if (ev == null || race == null) return NotFound();

            ViewData["EventId"] = eventId;
            ViewData["RaceId"] = raceId;

            return View(race);
        }

        // GET: /Events/Classes/Create?eventId=...&raceId=...
        public IActionResult CreateClass(string eventId, string raceId)
        {
            ViewData["EventId"] = eventId;
            ViewData["RaceId"] = raceId;
            return View(new Class { ScoringMethod = "Portsmouth", Rating = 1000.0 });
        }

        // POST: /Events/Classes/Create?eventId=...&raceId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(string eventId, string raceId, Class newClass)
        {
            if (!ModelState.IsValid)
            {
                ViewData["EventId"] = eventId;
                ViewData["RaceId"] = raceId;
                return View(newClass);
            }

            var events = await FileStorageHelper.LoadAsync<Event>(EventsFile);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var race = ev.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            newClass.Id = Guid.NewGuid().ToString();

            race.Classes.Add(newClass);
            await FileStorageHelper.SaveAsync(EventsFile, events);

            return RedirectToAction(nameof(Classes), new { eventId, raceId });
        }
    }
}
