using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SailingResultsPortal.Helpers;
using SailingResultsPortal.Models;
using SailingResultsPortal.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SailingResultsPortal.Controllers
{
    [Authorize(Roles = "Sudo,Organiser,Official")]
    public class ResultsController : Controller
    {
        private const string ResultsFile = "results.json";
        private const string EventsCacheKey = "events_data";
        private const string ResultsCacheKey = "results_data";
        private readonly ILogger<ResultsController> _logger;
        private readonly IMemoryCache _cache;

        public ResultsController(ILogger<ResultsController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Escape quotes and wrap in quotes if field contains comma, quote, or newline
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        private async Task<List<Event>> GetEventsAsync()
        {
            return await _cache.GetOrCreateAsync(EventsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await FileStorageHelper.LoadAsync<Event>("events.json") ?? new List<Event>();
            }) ?? new List<Event>();
        }

        private async Task<List<Result>> GetResultsAsync()
        {
            return await _cache.GetOrCreateAsync(ResultsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return await FileStorageHelper.LoadAsync<Result>(ResultsFile) ?? new List<Result>();
            }) ?? new List<Result>();
        }

        private void InvalidateCache()
        {
            _cache.Remove(EventsCacheKey);
            _cache.Remove(ResultsCacheKey);
        }

        // Helper method to format seconds as HH:MM:SS
        public static string FormatTimeFromSeconds(double seconds)
        {
            if (double.IsInfinity(seconds) || seconds >= double.MaxValue / 2)
                return "-";

            var timeSpan = TimeSpan.FromSeconds(Math.Round(seconds));
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        // GET: /Results/Public?eventId=...&filter=...
        [AllowAnonymous]
        public async Task<IActionResult> Public(string eventId, string filter)
        {
            var allEvents = await FileStorageHelper.LoadAsync<Event>("events.json");

            // If no eventId provided, show list of public events
            if (string.IsNullOrEmpty(eventId))
            {
                var publicEvents = allEvents.Where(e => e.IsPublic).ToList();
                ViewData["PublicEvents"] = publicEvents;
                return View("EventList");
            }

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var eventResults = allResults.Where(r => ev.Races.Any(race => race.Id == r.RaceId))
                                          .ToList();

            // Group by handicap system, then by class
            List<dynamic> groupedResults = new List<dynamic>();
            var handicapGroups = eventResults.GroupBy(r => ev.Races.First(race => race.Id == r.RaceId).HandicapSystem);
            foreach (var hg in handicapGroups)
            {
                var classes = hg.GroupBy(r => r.ClassId).Select(classGroup => new
                {
                    ClassId = classGroup.Key,
                    ClassName = string.IsNullOrEmpty(classGroup.Key)
                        ? "One Design"
                        : ev.Races.First(race => race.Id == classGroup.First().RaceId)
                              .Classes.FirstOrDefault(c => c.Id == classGroup.Key)?.Name ?? "Unknown Class",
                    Results = classGroup.OrderBy(r => r.Position).Select(r =>
                    {
                        return new
                        {
                            Id = r.Id,
                            RaceId = r.RaceId,
                            ClassId = r.ClassId,
                            SailorName = r.SailorName,
                            SailNumber = r.SailNumber,
                            FinishTime = r.FinishTime,
                            CorrectedTime = r.CorrectedTime,
                            Position = r.Position,
                            Status = r.Status,
                            Points = r.Points,
                            HandicapNumber = r.HandicapNumber
                        };
                    }).ToList()
                }).ToList();
                groupedResults.Add(new { HandicapSystem = hg.Key, Classes = classes });
            }

            // Apply filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                try
                {
                    var parsedFilter = HandicapFilterParser.Parse(filter);
                    var newGroupedResults = new List<dynamic>();
                    foreach (var handicapGroup in groupedResults)
                    {
                        if (handicapGroup.HandicapSystem != parsedFilter.HandicapType)
                        {
                            continue;
                        }

                        var filteredClasses = new List<dynamic>();
                        foreach (var classGroup in handicapGroup.Classes)
                        {
                            var race = ev.Races.First(r => r.Id == classGroup.Results.First().RaceId);

                            // Skip filtering for one design races or if class doesn't exist
                            if (string.IsNullOrEmpty(classGroup.ClassId) || !race.Classes.Any(c => c.Id == classGroup.ClassId))
                            {
                                if (parsedFilter.ClassName == "One Design" || string.IsNullOrEmpty(parsedFilter.ClassName))
                                {
                                    filteredClasses.Add(classGroup);
                                }
                                continue;
                            }

                            var cls = race.Classes.First(c => c.Id == classGroup.ClassId);
                            if (cls.Name != parsedFilter.ClassName) continue;

                            double rating = cls.Rating;
                            bool matches = false;
                            if (parsedFilter.Inequality == "=")
                                matches = rating == parsedFilter.Value1;
                            else if (parsedFilter.Inequality == "<")
                                matches = rating < parsedFilter.Value1;
                            else if (parsedFilter.Inequality == ">")
                                matches = rating > parsedFilter.Value1;
                            else if (parsedFilter.Inequality == "between")
                                matches = rating >= parsedFilter.Value1 && rating <= parsedFilter.Value2;

                            if (matches)
                            {
                                filteredClasses.Add(classGroup);
                            }
                        }

                        if (filteredClasses.Any())
                        {
                            newGroupedResults.Add(new { HandicapSystem = handicapGroup.HandicapSystem, Classes = filteredClasses });
                        }
                    }
                    groupedResults = newGroupedResults;
                }
                catch
                {
                    // Invalid filter, ignore
                }
            }

            ViewData["EventId"] = eventId;
            ViewData["GroupedResults"] = groupedResults;
            ViewData["IsPublic"] = true;

            return View("Index");
        }

        // GET: /Results?eventId=...&filter=...
        [Authorize(Roles = "Sudo,Organiser,Official")]
        public async Task<IActionResult> Index(string eventId, string filter)
        {
            // TODO: Add export functionality for human-readable CSV and machine-readable JSON download
            // TODO: Track who uploaded/amended results for audit trail
            // TODO: Add edit/delete functionality for results (organiser role)
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>("events.json");
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var resultsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Result>(ResultsFile);
            var eventResults = resultsWrapper.Data.Where(r => ev.Races.Any(race => race.Id == r.RaceId))
                                                   .ToList();

            // Store timestamps for client-side caching
            ViewData["EventsTimestamp"] = eventsWrapper.LastUpdated.ToString("o");
            ViewData["ResultsTimestamp"] = resultsWrapper.LastUpdated.ToString("o");

            // Group by handicap system, then by class
            List<dynamic> groupedResults = new List<dynamic>();
            var handicapGroups = eventResults.GroupBy(r => ev.Races.First(race => race.Id == r.RaceId).HandicapSystem);
            foreach (var hg in handicapGroups)
            {
                var classes = hg.GroupBy(r => r.ClassId).Select(classGroup => new
                {
                    ClassId = classGroup.Key,
                    ClassName = string.IsNullOrEmpty(classGroup.Key)
                        ? "One Design"
                        : ev.Races.First(race => race.Id == classGroup.First().RaceId)
                              .Classes.FirstOrDefault(c => c.Id == classGroup.Key)?.Name ?? "Unknown Class",
                    Results = classGroup.OrderBy(r => r.Position).Select(r =>
                    {
                        return new
                        {
                            Id = r.Id,
                            RaceId = r.RaceId,
                            ClassId = r.ClassId,
                            SailorName = r.SailorName,
                            SailNumber = r.SailNumber,
                            FinishTime = r.FinishTime,
                            CorrectedTime = r.CorrectedTime,
                            Position = r.Position,
                            Status = r.Status,
                            Points = r.Points,
                            HandicapNumber = r.HandicapNumber
                        };
                    }).ToList()
                }).ToList();
                groupedResults.Add(new { HandicapSystem = hg.Key, Classes = classes });
            }

            // Apply filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                try
                {
                    var parsedFilter = HandicapFilterParser.Parse(filter);
                    var newGroupedResults = new List<dynamic>();
                    foreach (var handicapGroup in groupedResults)
                    {
                        if (handicapGroup.HandicapSystem != parsedFilter.HandicapType)
                        {
                            continue;
                        }

                        var filteredClasses = new List<dynamic>();
                        foreach (var classGroup in handicapGroup.Classes)
                        {
                            var race = ev.Races.First(r => r.Id == classGroup.Results.First().RaceId);

                            // Skip filtering for one design races or if class doesn't exist
                            if (string.IsNullOrEmpty(classGroup.ClassId) || !race.Classes.Any(c => c.Id == classGroup.ClassId))
                            {
                                if (parsedFilter.ClassName == "One Design" || string.IsNullOrEmpty(parsedFilter.ClassName))
                                {
                                    filteredClasses.Add(classGroup);
                                }
                                continue;
                            }

                            var cls = race.Classes.First(c => c.Id == classGroup.ClassId);
                            if (cls.Name != parsedFilter.ClassName) continue;

                            double rating = cls.Rating;
                            bool matches = false;
                            if (parsedFilter.Inequality == "=")
                                matches = rating == parsedFilter.Value1;
                            else if (parsedFilter.Inequality == "<")
                                matches = rating < parsedFilter.Value1;
                            else if (parsedFilter.Inequality == ">")
                                matches = rating > parsedFilter.Value1;
                            else if (parsedFilter.Inequality == "between")
                                matches = rating >= parsedFilter.Value1 && rating <= parsedFilter.Value2;

                            if (matches)
                            {
                                filteredClasses.Add(classGroup);
                            }
                        }

                        if (filteredClasses.Any())
                        {
                            newGroupedResults.Add(new { HandicapSystem = handicapGroup.HandicapSystem, Classes = filteredClasses });
                        }
                    }
                    groupedResults = newGroupedResults;
                }
                catch
                {
                    // Invalid filter, ignore
                }
            }

            ViewData["EventId"] = eventId;
            ViewData["GroupedResults"] = groupedResults;

            return View();
        }

        // GET: /Results/BulkUpload?eventId=...&raceId=...
        public async Task<IActionResult> BulkUpload(string eventId, string? raceId = null)
        {
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            // If raceId is provided, validate it exists
            Race? selectedRace = null;
            if (!string.IsNullOrEmpty(raceId))
            {
                selectedRace = ev.Races.FirstOrDefault(r => r.Id == raceId);
                if (selectedRace == null) return NotFound();
            }

            ViewData["EventId"] = eventId;
            ViewData["Event"] = ev;
            ViewData["RaceId"] = raceId;
            ViewData["SelectedRace"] = selectedRace;
            return View();
        }

        // POST: /Results/BulkUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(string eventId, string raceId, string jsonData)
        {
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            if (string.IsNullOrEmpty(jsonData))
            {
                ModelState.AddModelError("jsonData", "JSON data is required");
                return View();
            }

            try
            {
                _logger.LogInformation("Processing bulk upload for event {EventId}, race {RaceId}", eventId, raceId);

                var events = await FileStorageHelper.LoadAsync<Event>("events.json");
                var ev = events.FirstOrDefault(e => e.Id == eventId);
                if (ev == null) return NotFound();

                // If raceId is provided, validate it exists
                Race? selectedRace = null;
                if (!string.IsNullOrEmpty(raceId))
                {
                    selectedRace = ev.Races.FirstOrDefault(r => r.Id == raceId);
                    if (selectedRace == null) return NotFound();
                }

                // Parse the JSON data - first try to parse with handicap numbers
                var uploadedResults = System.Text.Json.JsonSerializer.Deserialize<List<BulkResultDto>>(jsonData);
                if (uploadedResults == null || !uploadedResults.Any())
                {
                    ModelState.AddModelError("jsonData", "No valid results found in JSON data");
                    ViewData["EventId"] = eventId;
                    ViewData["Event"] = ev;
                    ViewData["RaceId"] = raceId;
                    ViewData["SelectedRace"] = selectedRace;
                    return View();
                }
                if (uploadedResults == null || !uploadedResults.Any())
                {
                    ModelState.AddModelError("jsonData", "No valid results found in JSON data");
                    ViewData["EventId"] = eventId;
                    ViewData["Event"] = ev;
                    return View();
                }

                var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
                var processedCount = 0;
                var errors = new List<string>();

                foreach (var dto in uploadedResults)
                {
                    try
                    {
                        // If raceId is provided via URL, use it automatically
                        if (!string.IsNullOrEmpty(raceId))
                        {
                            dto.RaceId = raceId;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(dto.RaceId) ||
                            string.IsNullOrWhiteSpace(dto.SailorName) ||
                            string.IsNullOrWhiteSpace(dto.SailNumber))
                        {
                            errors.Add($"Missing required fields for result: RaceId{(string.IsNullOrEmpty(raceId) ? "" : " (or provide via URL)") }, SailorName, or SailNumber");
                            continue;
                        }

                        // Check if race exists in the event
                        var race = ev.Races.FirstOrDefault(r => r.Id == dto.RaceId);
                        if (race == null)
                        {
                            errors.Add($"Race '{dto.RaceId}' not found in event {eventId}. Available race IDs: {string.Join(", ", ev.Races.Select(r => r.Id))}");
                            continue;
                        }

                        // Convert DTO to Result
                        var result = dto.ToResult();

                        // Handle ClassId - allow both class name and class ID
                        if (!string.IsNullOrWhiteSpace(dto.ClassId))
                        {
                            // First try to find by ID
                            var classById = race.Classes.FirstOrDefault(c => c.Id == dto.ClassId);
                            if (classById != null)
                            {
                                result.ClassId = dto.ClassId; // Keep the ID as is
                            }
                            else
                            {
                                // Try to find by name
                                var classByName = race.Classes.FirstOrDefault(c => c.Name == dto.ClassId);
                                if (classByName != null)
                                {
                                    result.ClassId = classByName.Id; // Convert name to ID
                                }
                                else
                                {
                                    errors.Add($"Class '{dto.ClassId}' not found in race {race.Id}. Available classes: {string.Join(", ", race.Classes.Select(c => $"{c.Name} ({c.Id})"))}");
                                    continue;
                                }
                            }
                        }

                        // Set defaults and audit info
                        result.Id = Guid.NewGuid().ToString();
                        result.Position = 1; // Will be recalculated
                        result.UploadedBy = User.Identity?.Name ?? "Unknown";
                        result.UploadedAt = DateTime.UtcNow;

                        // Calculate corrected time if finish time is provided
                        if (result.FinishTime.HasValue)
                        {
                            if (race.HandicapType == "Open")
                            {
                                if (dto.HandicapNumber.HasValue && dto.HandicapNumber > 0)
                                {
                                    // Calculate corrected time using handicap
                                    switch (race.HandicapSystem)
                                    {
                                        case "Portsmouth":
                                            result.CorrectedTime = ScoringService.CalculatePortsmouthCorrectedTime(result.FinishTime.Value, dto.HandicapNumber.Value).TotalSeconds;
                                            break;
                                        case "IRC":
                                            result.CorrectedTime = ScoringService.CalculateIRCCorrectedTime(result.FinishTime.Value, dto.HandicapNumber.Value).TotalSeconds;
                                            break;
                                        case "YTC":
                                            result.CorrectedTime = ScoringService.CalculateYTCCorrectedTime(result.FinishTime.Value, dto.HandicapNumber.Value).TotalSeconds;
                                            break;
                                        default:
                                            result.CorrectedTime = result.FinishTime.Value.TotalSeconds;
                                            break;
                                    }
                                }
                                else
                                {
                                    errors.Add($"Handicap number is required for Open handicap race but not provided for sailor {dto.SailorName}");
                                    continue;
                                }
                            }
                            else // OneDesign
                            {
                                result.CorrectedTime = result.FinishTime.Value.TotalSeconds;
                            }
                        }
                        else
                        {
                            result.CorrectedTime = double.MaxValue; // DNS/DNC/DNF/RET
                        }

                        allResults.Add(result);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error processing result for {dto.SailorName}: {ex.Message}");
                    }
                }

                if (processedCount > 0)
                {
                    // Recalculate positions and points for all affected races
                    foreach (var race in ev.Races)
                    {
                        var raceResults = allResults.Where(r => r.RaceId == race.Id).ToList();
                        if (raceResults.Any())
                        {
                            // Recalculate positions
                            var orderedResults = raceResults.OrderBy(r => r.CorrectedTime).ToList();
                            for (int i = 0; i < orderedResults.Count; i++)
                            {
                                orderedResults[i].Position = i + 1;
                            }

                            // Calculate points
                            ScoringService.CalculateRacePoints(raceResults, race);
                        }
                    }

                    await FileStorageHelper.SaveWithTimestampAsync(ResultsFile, allResults);
                    InvalidateCache();

                    _logger.LogInformation("Successfully processed {Count} results for bulk upload", processedCount);
                }

                ViewData["EventId"] = eventId;
                ViewData["Event"] = ev;
                ViewData["ProcessedCount"] = processedCount;
                ViewData["Errors"] = errors;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk upload for event {EventId}", eventId);
                ModelState.AddModelError("", "An error occurred while processing the bulk upload. Please check your JSON format.");
                return View();
            }
        }

        // GET: /Results/DownloadTemplate?eventId=...&raceId=...
        public async Task<IActionResult> DownloadTemplate(string eventId, string? raceId = null)
        {
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            // Create sample results
            var sampleResults = new List<BulkResultDto>();

            if (!string.IsNullOrEmpty(raceId))
            {
                // Generate template for specific race - omit RaceId
                var race = ev.Races.FirstOrDefault(r => r.Id == raceId);
                if (race != null)
                {
                    var sampleResult = new BulkResultDto
                    {
                        // RaceId omitted - will be set automatically
                        ClassId = race.Classes.Any() ? race.Classes.First().Name : "",
                        SailorName = "John Doe",
                        SailNumber = "ABC123",
                        FinishTime = "00:45:30",
                        Status = null // Finished
                    };

                    // Add handicap number for Open handicap races
                    if (race.HandicapType == "Open" && race.Classes.Any())
                    {
                        sampleResult.HandicapNumber = race.Classes.First().Rating;
                    }

                    sampleResults.Add(sampleResult);

                    // Add a DNS example
                    var dnsResult = new BulkResultDto
                    {
                        // RaceId omitted - will be set automatically
                        ClassId = race.Classes.Any() ? race.Classes.First().Name : "",
                        SailorName = "Jane Smith",
                        SailNumber = "DEF456",
                        FinishTime = null,
                        Status = "DNS"
                    };

                    // Add handicap number for DNS too
                    if (race.HandicapType == "Open" && race.Classes.Any())
                    {
                        dnsResult.HandicapNumber = race.Classes.First().Rating;
                    }

                    sampleResults.Add(dnsResult);
                }
            }
            else
            {
                // Generate template for entire event - include RaceId
                foreach (var race in ev.Races)
                {
                    var sampleResult = new BulkResultDto
                    {
                        RaceId = race.Id,
                        ClassId = race.Classes.Any() ? race.Classes.First().Name : "",
                        SailorName = "John Doe",
                        SailNumber = "ABC123",
                        FinishTime = "00:45:30",
                        Status = null // Finished
                    };

                    // Add handicap number for Open handicap races
                    if (race.HandicapType == "Open" && race.Classes.Any())
                    {
                        sampleResult.HandicapNumber = race.Classes.First().Rating;
                    }

                    sampleResults.Add(sampleResult);

                    // Add a DNS example
                    var dnsResult = new BulkResultDto
                    {
                        RaceId = race.Id,
                        ClassId = race.Classes.Any() ? race.Classes.First().Name : "",
                        SailorName = "Jane Smith",
                        SailNumber = "DEF456",
                        FinishTime = null,
                        Status = "DNS"
                    };

                    // Add handicap number for DNS too
                    if (race.HandicapType == "Open" && race.Classes.Any())
                    {
                        dnsResult.HandicapNumber = race.Classes.First().Rating;
                    }

                    sampleResults.Add(dnsResult);
                }
            }

            var json = JsonSerializer.Serialize(sampleResults, new JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"results_template_{eventId}.json");
        }

        // GET: /Results/Create?raceId=...&classId=...
        public async Task<IActionResult> Create(string raceId, string classId)
        {
            if (string.IsNullOrEmpty(raceId))
                return BadRequest("RaceId is required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Races.Any(r => r.Id == raceId));
            if (ev == null) return NotFound();

            var race = ev.Races.FirstOrDefault(r => r.Id == raceId);
            if (race == null) return NotFound();

            ViewData["HandicapType"] = race.HandicapType;
            ViewData["HandicapSystem"] = race.HandicapSystem;
            ViewData["EventId"] = ev.Id;

            var model = new Result
            {
                RaceId = raceId,
                ClassId = classId,
                Position = 1 // Set default valid position to pass validation
            };
            return View(model);
        }

        // POST: /Results/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Result result, string finishTimeInput, double? handicapNumber)
        {
            try
            {
                _logger.LogInformation("Creating result for race {RaceId}, sailor {SailorName}", result.RaceId, result.SailorName);
                _logger.LogInformation("FinishTimeInput: {FinishTimeInput}, HandicapNumber: {HandicapNumber}", finishTimeInput, handicapNumber);
                _logger.LogInformation("Result model: RaceId={RaceId}, ClassId={ClassId}, SailorName={SailorName}", result.RaceId, result.ClassId, result.SailorName);

                TimeSpan? finishTime = null;
                if (!string.IsNullOrEmpty(finishTimeInput))
                {
                    // Try multiple time formats for better user experience
                    string[] formats = { @"hh\:mm\:ss", @"h\:mm\:ss", @"mm\:ss", @"m\:ss", "c" };
                    bool parsed = false;

                    foreach (string format in formats)
                    {
                        if (TimeSpan.TryParseExact(finishTimeInput.Trim(), format, CultureInfo.InvariantCulture, out var parsedTime))
                        {
                            finishTime = parsedTime;
                            parsed = true;
                            break;
                        }
                    }

                    if (!parsed)
                    {
                        ModelState.AddModelError("FinishTime", "Invalid finish time format. Use hh:mm:ss, h:mm:ss, mm:ss, or m:ss.");
                    }
                }

                result.FinishTime = finishTime;
                // Status is already bound to the model via asp-for="Status"

                // Set temporary valid Position for validation (will be recalculated after saving)
                result.Position = 1;

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed for result creation");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                    }
                    return View(result);
                }

                result.Id = Guid.NewGuid().ToString();
                result.HandicapNumber = handicapNumber; // Store the handicap number in the result
                result.UploadedBy = User.Identity?.Name ?? "Unknown";
                result.UploadedAt = DateTime.UtcNow;

                var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>("events.json");
                var ev = eventsWrapper.Data.FirstOrDefault(e => e.Races.Any(r => r.Id == result.RaceId));
                if (ev == null)
                {
                    _logger.LogWarning("Event not found for race {RaceId}", result.RaceId);
                    return NotFound();
                }

                var race = ev.Races.FirstOrDefault(r => r.Id == result.RaceId);
                if (race == null)
                {
                    _logger.LogWarning("Race not found: {RaceId}", result.RaceId);
                    return NotFound();
                }

                if (result.FinishTime.HasValue)
                {
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
                                result.CorrectedTime = ScoringService.CalculatePortsmouthCorrectedTime(result.FinishTime.Value, handicapNumber.Value).TotalSeconds;
                                break;
                            case "IRC":
                                result.CorrectedTime = ScoringService.CalculateIRCCorrectedTime(result.FinishTime.Value, handicapNumber.Value).TotalSeconds;
                                break;
                            case "YTC":
                                result.CorrectedTime = ScoringService.CalculateYTCCorrectedTime(result.FinishTime.Value, handicapNumber.Value).TotalSeconds;
                                break;
                            default:
                                result.CorrectedTime = result.FinishTime.Value.TotalSeconds;
                                break;
                        }
                    }
                    else // OneDesign
                    {
                        result.CorrectedTime = result.FinishTime.Value.TotalSeconds;
                    }
                }
                else
                {
                    // For DNS/DNC/DNF/RET cases, set corrected time to a large value for sorting
                    result.CorrectedTime = double.MaxValue;
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

                // Calculate points for all results in this race
                var raceResults = allResults.Where(r => r.RaceId == result.RaceId).ToList();
                ScoringService.CalculateRacePoints(raceResults, race);

                await FileStorageHelper.SaveWithTimestampAsync(ResultsFile, allResults);
                InvalidateCache(); // Clear cache since data was modified
    
                _logger.LogInformation("Successfully created result {ResultId} for race {RaceId}", result.Id, result.RaceId);
    
                return RedirectToAction(nameof(Index), new { eventId = ev.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating result for race {RaceId}", result.RaceId);
                ModelState.AddModelError("", "An error occurred while saving the result. Please try again.");
                return View(result);
            }
        }

        // GET: /Results/ExportCsv?eventId=...
        [AllowAnonymous]
        public async Task<IActionResult> ExportCsv(string eventId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventId))
                {
                    _logger.LogWarning("ExportCsv called with empty eventId");
                    return BadRequest("EventId is required");
                }

                var events = await GetEventsAsync();
                var ev = events.FirstOrDefault(e => e.Id == eventId);
                if (ev == null)
                {
                    _logger.LogWarning("ExportCsv called with invalid eventId: {EventId}", eventId);
                    return NotFound();
                }

                // Check access for private events
                if (!ev.IsPublic)
                {
                    var userId = User.FindFirst("UserId")?.Value;
                    if (string.IsNullOrEmpty(userId) || !ev.AllowedUsers.Contains(userId))
                    {
                        _logger.LogWarning("Unauthorized access attempt to private event {EventId} by user {UserId}", eventId, userId);
                        return Forbid();
                    }
                }

                var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
                var eventResults = allResults.Where(r => ev.Races.Any(race => race.Id == r.RaceId)).ToList();

                _logger.LogInformation("Exporting {Count} results for event {EventId}", eventResults.Count, eventId);

                var csv = "Position,Sailor Name,Sail Number,PY Number,Finish Time,Corrected Time,Uploaded By,Uploaded At\n";
                foreach (var result in eventResults.OrderBy(r => r.Position))
                {
                    string handicapStr = result.HandicapNumber?.ToString() ?? "-";

                    var finishTimeStr = result.FinishTime.HasValue ? result.FinishTime.Value.ToString(@"hh\:mm\:ss") : "DNS/DNC/DNF/RET";
                    var correctedTimeStr = FormatTimeFromSeconds(result.CorrectedTime);
                    csv += $"{result.Position},{EscapeCsvField(result.SailorName)},{EscapeCsvField(result.SailNumber)},{handicapStr},{finishTimeStr},{correctedTimeStr},{EscapeCsvField(result.UploadedBy)},{result.UploadedAt:yyyy-MM-dd HH:mm:ss}\n";
                }

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"results_{eventId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting CSV for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while generating the export");
            }
        }

        // GET: /Results/ExportJson?eventId=...
        [AllowAnonymous]
        public async Task<IActionResult> ExportJson(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var eventResults = allResults.Where(r => ev.Races.Any(race => race.Id == r.RaceId)).ToList();

            return File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(eventResults), "application/json", $"results_{eventId}.json");
        }

        // GET: /Results/Edit?id=...
        [Authorize(Roles = "Sudo,Organiser")]
        public async Task<IActionResult> Edit(string id)
        {
            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var result = allResults.FirstOrDefault(r => r.Id == id);
            if (result == null) return NotFound();

            // Find the event for this result
            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Races.Any(r => r.Id == result.RaceId));
            if (ev != null)
            {
                ViewData["EventId"] = ev.Id;
            }

            return View(result);
        }

        // POST: /Results/Edit
        [HttpPost]
        [Authorize(Roles = "Sudo,Organiser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Result updatedResult, string finishTimeInput, double? handicapNumber)
        {
            TimeSpan? finishTime = null;
            if (!string.IsNullOrEmpty(finishTimeInput))
            {
                // Try multiple time formats for better user experience
                string[] formats = { @"hh\:mm\:ss", @"h\:mm\:ss", @"mm\:ss", @"m\:ss", "c" };
                bool parsed = false;

                foreach (string format in formats)
                {
                    if (TimeSpan.TryParseExact(finishTimeInput.Trim(), format, CultureInfo.InvariantCulture, out var parsedTime))
                    {
                        finishTime = parsedTime;
                        parsed = true;
                        break;
                    }
                }

                if (!parsed)
                {
                    ModelState.AddModelError("FinishTime", "Invalid finish time format. Use hh:mm:ss, h:mm:ss, mm:ss, or m:ss.");
                }
            }

            if (!ModelState.IsValid)
                return View(updatedResult);

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var result = allResults.FirstOrDefault(r => r.Id == id);
            if (result == null) return NotFound();

            result.SailorName = updatedResult.SailorName;
            result.SailNumber = updatedResult.SailNumber;
            result.FinishTime = finishTime;
            result.HandicapNumber = handicapNumber; // Store the handicap number in the result
            result.AmendedBy = User.Identity?.Name ?? "Unknown";
            result.AmendedAt = DateTime.UtcNow;

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Races.Any(r => r.Id == result.RaceId));
            var race = ev?.Races.FirstOrDefault(r => r.Id == result.RaceId);

            if (result.FinishTime.HasValue && race != null && race.HandicapType == "Open" && handicapNumber.HasValue)
            {
                switch (race.HandicapSystem)
                {
                    case "Portsmouth":
                        result.CorrectedTime = ScoringService.CalculatePortsmouthCorrectedTime(result.FinishTime.Value, handicapNumber.Value).TotalSeconds;
                        break;
                    case "IRC":
                        result.CorrectedTime = ScoringService.CalculateIRCCorrectedTime(result.FinishTime.Value, handicapNumber.Value).TotalSeconds;
                        break;
                    case "YTC":
                        result.CorrectedTime = ScoringService.CalculateYTCCorrectedTime(result.FinishTime.Value, handicapNumber.Value).TotalSeconds;
                        break;
                }
            }
            else if (!result.FinishTime.HasValue)
            {
                // For DNS/DNC/DNF/RET cases, set corrected time to a large value for sorting
                result.CorrectedTime = double.MaxValue;
            }

            // Recalculate positions
            var filteredResults = allResults.Where(r => r.RaceId == result.RaceId && r.ClassId == result.ClassId)
                                            .OrderBy(r => r.CorrectedTime).ToList();
            for (int i = 0; i < filteredResults.Count; i++)
            {
                filteredResults[i].Position = i + 1;
            }

            await FileStorageHelper.SaveWithTimestampAsync(ResultsFile, allResults);

            return RedirectToAction(nameof(Index), new { eventId = ev?.Id });
        }

        // POST: /Results/Delete?id=...
        [HttpPost]
        [Authorize(Roles = "Sudo,Organiser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var result = allResults.FirstOrDefault(r => r.Id == id);
            if (result == null) return NotFound();

            allResults.Remove(result);

            // Recalculate positions
            var filteredResults = allResults.Where(r => r.RaceId == result.RaceId && r.ClassId == result.ClassId)
                                            .OrderBy(r => r.CorrectedTime).ToList();
            for (int i = 0; i < filteredResults.Count; i++)
            {
                filteredResults[i].Position = i + 1;
            }

            await FileStorageHelper.SaveWithTimestampAsync(ResultsFile, allResults);

            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>("events.json");
            var ev = eventsWrapper.Data.FirstOrDefault(e => e.Races.Any(r => r.Id == result.RaceId));

            return RedirectToAction(nameof(Index), new { eventId = ev?.Id });
        }

        // GET: /Results/Overall?eventId=...
        [AllowAnonymous]
        public async Task<IActionResult> Overall(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            // Check access for private events
            if (!ev.IsPublic)
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !ev.AllowedUsers.Contains(userId))
                {
                    return Forbid();
                }
            }

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var eventResults = allResults.Where(r => ev.Races.Any(race => race.Id == r.RaceId)).ToList();

            // Calculate overall points
            var overallPoints = new Dictionary<string, (int TotalPoints, List<(string RaceName, int Points)> RaceDetails)>();

            foreach (var result in eventResults)
            {
                var sailorKey = result.SailorName;
                var race = ev.Races.FirstOrDefault(r => r.Id == result.RaceId);

                if (!overallPoints.ContainsKey(sailorKey))
                {
                    overallPoints[sailorKey] = (0, new List<(string, int)>());
                }

                var current = overallPoints[sailorKey];
                current.RaceDetails.Add((race?.Name ?? "Unknown Race", result.Points));
                overallPoints[sailorKey] = current;
            }

            // Apply discards to overall points
            var finalOverallPoints = new Dictionary<string, (int TotalPoints, List<(string RaceName, int Points)> RaceDetails)>();
            foreach (var sailor in overallPoints)
            {
                var racePoints = sailor.Value.RaceDetails.Select(rd => rd.Points).ToList();
                var totalPoints = ScoringService.CalculateOverallPointsWithDiscards(
                    racePoints, ev.RacesBeforeDiscards, ev.NumberOfDiscards);
                finalOverallPoints[sailor.Key] = (totalPoints, sailor.Value.RaceDetails);
            }

            ViewData["EventId"] = eventId;
            ViewData["Event"] = ev;
            ViewData["OverallPoints"] = finalOverallPoints.OrderBy(kvp => kvp.Value.TotalPoints).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return View();
        }

        // GET: /Results/Overview?eventId=...
        [AllowAnonymous]
        public async Task<IActionResult> Overview(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return BadRequest("EventId is required");

            var events = await FileStorageHelper.LoadAsync<Event>("events.json");
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound();

            // Check access for private events
            if (!ev.IsPublic)
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !ev.AllowedUsers.Contains(userId))
                {
                    return Forbid();
                }
            }

            var allResults = await FileStorageHelper.LoadAsync<Result>(ResultsFile);
            var eventResults = allResults.Where(r => ev.Races.Any(race => race.Id == r.RaceId)).ToList();

            // Group results by class and race
            var classOverview = new Dictionary<string, Dictionary<string, List<Result>>>();

            foreach (var result in eventResults)
            {
                var race = ev.Races.FirstOrDefault(r => r.Id == result.RaceId);
                if (race == null) continue;

                var className = string.IsNullOrEmpty(result.ClassId)
                    ? "One Design"
                    : race.Classes.FirstOrDefault(c => c.Id == result.ClassId)?.Name ?? "Unknown Class";

                if (!classOverview.ContainsKey(className))
                {
                    classOverview[className] = new Dictionary<string, List<Result>>();
                }

                if (!classOverview[className].ContainsKey(race.Name))
                {
                    classOverview[className][race.Name] = new List<Result>();
                }

                classOverview[className][race.Name].Add(result);
            }

            // Calculate overall standings for each class
            var classStandings = new Dictionary<string, List<(string SailorName, int TotalPoints, List<(string RaceName, int Points)> RaceBreakdown)>>();

            foreach (var classEntry in classOverview)
            {
                var className = classEntry.Key;
                var sailorPoints = new Dictionary<string, (int Total, List<(string Race, int Points)> Breakdown)>();

                foreach (var raceEntry in classEntry.Value)
                {
                    var raceName = raceEntry.Key;
                    var raceResults = raceEntry.Value.OrderBy(r => r.Position).ToList();

                    for (int i = 0; i < raceResults.Count; i++)
                    {
                        var result = raceResults[i];
                        var sailorName = result.SailorName;

                        if (!sailorPoints.ContainsKey(sailorName))
                        {
                            sailorPoints[sailorName] = (0, new List<(string, int)>());
                        }

                        var current = sailorPoints[sailorName];
                        current.Total += result.Points;
                        current.Breakdown.Add((raceName, result.Points));
                        sailorPoints[sailorName] = current;
                    }
                }

                // Apply discards to class standings
                var sailorsWithDiscards = new List<(string SailorName, int TotalPoints, List<(string RaceName, int Points)> RaceBreakdown)>();
                foreach (var sailor in sailorPoints)
                {
                    var racePoints = sailor.Value.Breakdown.Select(rb => rb.Points).ToList();
                    var totalPoints = ScoringService.CalculateOverallPointsWithDiscards(
                        racePoints, ev.RacesBeforeDiscards, ev.NumberOfDiscards);
                    sailorsWithDiscards.Add((sailor.Key, totalPoints, sailor.Value.Breakdown));
                }

                classStandings[className] = sailorsWithDiscards.OrderBy(s => s.TotalPoints).ToList();
            }

            ViewData["EventId"] = eventId;
            ViewData["Event"] = ev;
            ViewData["ClassOverview"] = classOverview;
            ViewData["ClassStandings"] = classStandings;

            return View();
        }
    }
}
