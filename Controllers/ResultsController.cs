using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Helpers;
using SailingResultsPortal.Models;
using System;
using System.Collections.Generic;
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
        public IActionResult Create(string raceId, string classId)
        {
            if (string.IsNullOrEmpty(raceId) || string.IsNullOrEmpty(classId))
                return BadRequest("RaceId and ClassId are required");

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
        public async Task<IActionResult> Create(Result result, string finishTimeInput)
        {
            if (!TimeSpan.TryParseExact(finishTimeInput, "c", CultureInfo.InvariantCulture, out var finishTime))
            {
                ModelState.AddModelError("FinishTime", "Invalid finish time format. Use hh:mm:ss or mm:ss.");
            }

            if (!ModelState.IsValid)
            {
                return View(result);
            }

            result.FinishTime = finishTime;
            result.Id = Guid.NewGuid().ToString();

            // Calculate corrected time placeholder (TODO: implement real scoring)
            result.CorrectedTime = CalculateCorrectedTime(result);

            // Position is assigned based on corrected time (will recalc all on save)
            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            allResults.Add(result);

            // Recalculate positions for this race/class after adding
            var filteredResults = allResults.Where(r => r.RaceId == result.RaceId && r.ClassId == result.ClassId)
                                            .OrderBy(r => r.CorrectedTime)
                                            .ToList();
            for (int i = 0; i < filteredResults.Count; i++)
            {
                filteredResults[i].Position = i + 1;
            }

            // Save updated results
            await FileStorageHelper.SaveAsync(ResultsFile, allResults);

            return RedirectToAction(nameof(Index), new { raceId = result.RaceId, classId = result.ClassId });
        }

        // TODO: Add Edit and Delete actions for results

        private double CalculateCorrectedTime(Result r)
        {
            // TODO: Implement according to scoring method of the class

            // Temporary: use raw finish time in seconds as corrected time
            return r.FinishTime.TotalSeconds;
        }
    }
}
