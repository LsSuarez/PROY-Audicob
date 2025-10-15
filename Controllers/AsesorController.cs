// Este es el código corregido para el controlador AsesorController

using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Asesor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Audicob.Controllers
{
    [Authorize(Roles = "AsesorCobranza")]
    public class AsesorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AsesorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Panel del asesor
        [HttpGet]
        public async Task<IActionResult> Dashboard(string searchTerm = "")
        {
            // Obtén el usuario actual
            var user = await _userManager.GetUserAsync(User);

            // Obtener los clientes asignados al asesor
            var asignaciones = await _db.AsignacionesAsesores
                .Include(a => a.Cliente) // Incluye la relación con Cliente
                .Where(a => a.AsesorUserId == user.Id)
                .ToListAsync();

            // Filtrar los clientes si hay un término de búsqueda
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim().ToLower(); // Convertir a minúsculas para evitar problemas de mayúsculas/minúsculas
                asignaciones = asignaciones.Where(a =>
                    (a.Cliente.Nombre?.ToLower() ?? "").Contains(t) || // Buscar en el nombre
                    (a.Cliente.Documento?.ToLower() ?? "").Contains(t)) // Buscar en el documento (DNI)
                    .ToList();
            }

            // Crear el modelo para la vista
            var vm = new AsesorDashboardViewModel
            {
                SearchTerm = searchTerm, // Pasar el término de búsqueda al modelo
                TotalClientesAsignados = asignaciones.Count,
                TotalDeudaCartera = asignaciones.Sum(a => a.Cliente.DeudaTotal),
                TotalPagosRecientes = await _db.Pagos
                    .Where(p => asignaciones.Select(a => a.ClienteId).Contains(p.ClienteId) &&
                                p.Fecha >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(p => p.Monto),
                Clientes = asignaciones.Select(a => a.Cliente.Nombre).ToList(),
                DeudasPorCliente = asignaciones.Select(a => a.Cliente.DeudaTotal).ToList(),
                ClientesAsignados = asignaciones.Select(a => new Audicob.Models.ViewModels.Asesor.ClienteResumen
                {
                    Nombre = a.Cliente.Nombre,
                    Documento = a.Cliente.Documento,
                    Deuda = a.Cliente.DeudaTotal,
                    IngresosMensuales = a.Cliente.IngresosMensuales,
                    FechaActualizacion = a.Cliente.FechaActualizacion,
                    ClienteId = a.Cliente.Id
                }).ToList()
            };

            // Pasar el modelo a la vista
            return View(vm);
        }
    }
}
