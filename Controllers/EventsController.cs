using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Helpers;
using SailingResultsPortal.Models;
using SailingResultsPortal.Services;
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
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            ViewData["EventsTimestamp"] = eventsWrapper.LastUpdated.ToString("o");
            return View(eventsWrapper.Data);
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

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            newEvent.Id = Guid.NewGuid().ToString();
            newEvent.Races = new();

            eventsWrapper.Data.Add(newEvent);
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == id);
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

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound();

            ev.Name = updatedEvent.Name;
            ev.RacesBeforeDiscards = updatedEvent.RacesBeforeDiscards;
            ev.NumberOfDiscards = updatedEvent.NumberOfDiscards;
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Races?eventId=...
        public async Task<IActionResult> Races(string eventId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
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

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            newRace.Id = Guid.NewGuid().ToString();
            newRace.Classes = new();

            ev.Races.Add(newRace);
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // GET: /Events/Races/Edit?eventId=...&raceId=...
        public async Task<IActionResult> EditRace(string eventId, string raceId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
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

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            var race = ev?.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            race.Name = updatedRace.Name;
            race.HandicapType = updatedRace.HandicapType;
            race.HandicapSystem = updatedRace.HandicapSystem;

            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // GET: /Events/Classes?eventId=...&raceId=...
        public async Task<IActionResult> Classes(string eventId, string raceId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
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

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var race = ev.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            newClass.Id = Guid.NewGuid().ToString();

            race.Classes.Add(newClass);
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Classes), new { eventId, raceId });
        }

        // GET: /Events/Team?eventId=...
        public async Task<IActionResult> Team(string eventId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var allUsers = await UserService.GetAllUsersAsync();
            var teamMembers = allUsers.Where(u => ev.TeamMembers.Contains(u.Id)).ToList();
            var availableUsers = allUsers.Where(u => !ev.TeamMembers.Contains(u.Id)).ToList();

            ViewData["EventId"] = eventId;
            ViewData["TeamMembers"] = teamMembers;
            ViewData["AvailableUsers"] = availableUsers;

            return View(ev);
        }

        // POST: /Events/AddTeamMember?eventId=...&userId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeamMember(string eventId, string userId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            if (!ev.TeamMembers.Contains(userId))
            {
                ev.TeamMembers.Add(userId);
                await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);
            }

            return RedirectToAction(nameof(Team), new { eventId });
        }

        // POST: /Events/RemoveTeamMember?eventId=...&userId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTeamMember(string eventId, string userId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            ev.TeamMembers.Remove(userId);
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Team), new { eventId });
        }

        // POST: /Events/TogglePrivacy?eventId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePrivacy(string eventId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            ev.IsPublic = !ev.IsPublic;
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // POST: /Events/AddAllowedUser?eventId=...&userId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllowedUser(string eventId, string userId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            if (!ev.AllowedUsers.Contains(userId))
            {
                ev.AllowedUsers.Add(userId);
                await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);
            }

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // POST: /Events/RemoveAllowedUser?eventId=...&userId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAllowedUser(string eventId, string userId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            ev.AllowedUsers.Remove(userId);
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Races), new { eventId });
        }

        // POST: /Events/DeleteClass?eventId=...&raceId=...&classId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClass(string eventId, string raceId, string classId)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>(EventsFile);
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var race = ev.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            var classToDelete = race.Classes.FirstOrDefault(c => c.Id == classId);
            if (classToDelete == null) return NotFound();

            race.Classes.Remove(classToDelete);
            await FileStorageHelper.SaveWithTimestampAsync(EventsFile, eventsWrapper.Data);

            return RedirectToAction(nameof(Classes), new { eventId, raceId });
        }
    }
}
