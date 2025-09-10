using Microsoft.AspNetCore.Mvc;

namespace SailingResultsPortal.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
