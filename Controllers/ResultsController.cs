using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Helpers;
using SailingResultsPortal.Models;
using SailingResultsPortal.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SailingResultsPortal.Controllers
{
    [Authorize(Roles = "Sudo,Organiser,Official")]
    public class ResultsController : Controller
    {
        private const string ResultsFile = "results.json";

        // GET: /Results?raceId=...&classId=...
        [AllowAnonymous]
        public async Task<IActionResult> Index(string raceId, string classId)
        {
            if (string.IsNullOrEmpty(raceId) || string.IsNullOrEmpty(classId))
                return BadRequest("RaceId and ClassId are required");

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var results = allResults.Where(r => r.RaceId == raceId && r.ClassId == classId)
                                   .OrderBy(r => r.Position)
                                   .ToList();

            return View(results);
        }

        // GET: /Results/Create?raceId=...&classId=...
        public async Task<IActionResult> Create(string raceId, string classId)
        {
            if (string.IsNullOrEmpty(raceId) || string.IsNullOrEmpty(classId))
                return BadRequest("RaceId and ClassId are required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Races.Any(r => r.Id == raceId));
            if (ev == null) return NotFound();

            var race = ev.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            ViewData["HandicapType"] = race.HandicapType;
            ViewData["HandicapSystem"] = race.HandicapSystem;

            var model = new Result
            {
                RaceId = raceId,
                ClassId = classId
            };
            return View(model);
        }

        // POST: /Results/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Result result, string finishTimeInput, double? handicapNumber)
        {
            if (!TimeSpan.TryParseExact(finishTimeInput, "c", CultureInfo.InvariantCulture, out var finishTime))
                ModelState.AddModelError("FinishTime", "Invalid finish time format. Use hh:mm:ss.");

            if (!ModelState.IsValid)
                return View(result);

            result.FinishTime = finishTime;
            result.Id = Guid.NewGuid().ToString();

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Races.Any(r => r.Id == result.RaceId));
            if (ev == null) return NotFound();

            var race = ev.Races.FirstOrDefault(r => r.Id == result.RaceId);
            if (race == null) return NotFound();

            if (race.HandicapType == "Open")
            {
                if (handicapNumber == null || handicapNumber <= 0)
                {
                    ModelState.AddModelError("HandicapNumber", "Handicap number is required and must be greater than zero.");
                    return View(result);
                }

                switch (race.HandicapSystem)
                {
                    case "Portsmouth":
                        result.CorrectedTime = ScoringService.CalculatePortsmouthCorrectedTime(result.FinishTime, handicapNumber.Value).TotalSeconds;
                        break;
                    case "IRC":
                        result.CorrectedTime = ScoringService.CalculateIRCCorrectedTime(result.FinishTime, handicapNumber.Value).TotalSeconds;
                        break;
                    case "YTC":
                        result.CorrectedTime = ScoringService.CalculateYTCCorrectedTime(result.FinishTime, handicapNumber.Value).TotalSeconds;
                        break;
                    default:
                        result.CorrectedTime = result.FinishTime.TotalSeconds;
                        break;
                }
            }
            else // OneDesign
            {
                result.CorrectedTime = result.FinishTime.TotalSeconds;
            }

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            allResults.Add(result);

            var filteredResults = allResults
                .Where(r => r.RaceId == result.RaceId && r.ClassId == result.ClassId)
                .OrderBy(r => r.CorrectedTime)
                .ToList();

            for (int i = 0; i < filteredResults.Count; i++)
            {
                filteredResults[i].Position = i + 1;
            }

            await FileStorageHelper.SaveAsync(ResultsFile, allResults);

            return RedirectToAction(nameof(Index), new { raceId = result.RaceId, classId = result.ClassId });
        }
    }
}
