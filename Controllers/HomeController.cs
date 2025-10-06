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

        public async Task<IActionResult> Index()
        {
            // Si el usuario ya está autenticado, redirigirlo a su panel
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Administrador")) return RedirectToAction("Dashboard", "Admin");
                if (roles.Contains("Supervisor")) return RedirectToAction("Dashboard", "Supervisor");
                if (roles.Contains("AsesorCobranza")) return RedirectToAction("Dashboard", "Asesor");
                if (roles.Contains("Cliente")) return RedirectToAction("Dashboard", "Cliente");
            }

            // Si no está autenticado, mostrar la página de bienvenida
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
