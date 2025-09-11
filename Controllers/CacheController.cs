using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Helpers;
using SailingResultsPortal.Models;
using System.Threading.Tasks;

namespace SailingResultsPortal.Controllers
{
    [ApiController]
    [Route("api/cache")]
    public class CacheController : ControllerBase
    {
        [HttpGet("timestamps")]
        public async Task<IActionResult> GetTimestamps(string? eventId = null)
        {
            var eventsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Event>("events.json");
            var resultsWrapper = await FileStorageHelper.LoadWithTimestampAsync<Result>("results.json");

            return Ok(new
            {
                events = eventsWrapper.LastUpdated.ToString("o"),
                results = resultsWrapper.LastUpdated.ToString("o")
            });
        }
    }
}