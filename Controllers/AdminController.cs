    using Audicob.Models;
    using Audicob.Models.ViewModels.Admin;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;  // Asegúrate de tener esto
    using System.Linq;
    using System.Threading.Tasks;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Acción para cargar el Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var vm = new AdminDashboardViewModel();

            try
            {
                // Cargar todos los usuarios asincrónicamente usando ToListAsync()
                var allUsers = await _userManager.Users.ToListAsync();
                vm.TotalUsuarios = allUsers.Count;

                // Obtener los roles disponibles de forma asincrónica usando ToListAsync()
                var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                vm.RolesDisponibles = roles;

                // Inicializar el diccionario para contar usuarios por rol
                vm.UsuariosPorRol = new Dictionary<string, int>();

                // Cargar la cantidad de usuarios por cada rol
                foreach (var role in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    vm.UsuariosPorRol[role] = usersInRole.Count;
                }

                // Calcular los usuarios activos, suspendidos y nuevos esta semana
                vm.CalcularUsuariosActivos(allUsers);
                vm.CalcularUsuariosNuevosEstaSemana(allUsers);
                vm.CalcularUsuariosSuspendidos(allUsers);

            }
            catch (Exception ex)
            {
                // Manejo de errores básicos para detectar problemas con la carga de datos
                ModelState.AddModelError("", "Ocurrió un error al cargar la información. Intenta nuevamente.");
            }

            return View(vm);
        }
    }
}