using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Models;
using System.Diagnostics;

namespace SailingResultsPortal.Controllers
{
    public class HomeController : Controller
    {
        // Landing page
        public IActionResult Index()
        {
            return View();
        }

        // Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Error page with request ID for diagnostics
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(errorModel);
        }
    }
}
