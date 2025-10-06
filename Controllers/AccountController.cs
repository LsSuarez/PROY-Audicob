using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Audicob.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            ILogger<AccountController> logger,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
        }

        // Vista Login
        public IActionResult Login() => View();

        // Procesar Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Credenciales inválidas.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation($"Usuario {user.Email} ha iniciado sesión correctamente.");

            // Redirigir según el rol
            if (roles.Contains("Administrador"))
                return RedirectToAction("Dashboard", "Admin");
            if (roles.Contains("Supervisor"))
                return RedirectToAction("Dashboard", "Supervisor");
            if (roles.Contains("AsesorCobranza"))
                return RedirectToAction("Dashboard", "Asesor");
            if (roles.Contains("Cliente"))
                return RedirectToAction("Dashboard", "Cliente");

            return RedirectToAction("Index", "Home");
        }

        // Vista Registro
        public IActionResult Register() => View();

        // Procesar Registro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View(model);
            }

            // Crear el nuevo usuario
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // Asignar el rol
            if (!string.IsNullOrEmpty(model.Role))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }
            }

            // Crear Cliente automáticamente si el rol es "Cliente"
            if (model.Role == "Cliente")
            {
                var cliente = new Cliente
                {
                    UserId = user.Id,
                    Documento = "",
                    Nombre = model.FullName,
                    IngresosMensuales = 0,
                    DeudaTotal = 0,
                    FechaActualizacion = DateTime.UtcNow
                };

                _db.Clientes.Add(cliente);
                await _db.SaveChangesAsync();
                
                _logger.LogInformation($"Cliente creado automáticamente para: {user.Email}");
            }

            _logger.LogInformation($"Nuevo usuario registrado: {user.Email}");
            TempData["Success"] = "¡Registro exitoso! Ahora puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        // Cerrar sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("El usuario ha cerrado sesión.");
            return RedirectToAction("Index", "Home");
        }

        // Acceso denegado
        public IActionResult AccessDenied() => View();
    }
}