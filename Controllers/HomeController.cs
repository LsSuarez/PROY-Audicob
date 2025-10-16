using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Audicob.Models;
using Microsoft.AspNetCore.Authorization;

namespace Audicob.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Ayuda()
        {
            // Página informativa o institucional
            return View();
        }
    }
}
