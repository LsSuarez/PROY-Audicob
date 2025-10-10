using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Asesor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var asignaciones = await _db.AsignacionesAsesores
                .Include(a => a.Clientes)
                .ThenInclude(c => c.Deuda)
                .Where(a => a.AsesorUserId == user.Id)
                .ToListAsync();

            var clientes = asignaciones.SelectMany(a => a.Clientes).ToList();

            var vm = new AsesorDashboardViewModel
            {
                TotalClientesAsignados = clientes.Count,
                TotalDeudaCartera = clientes.Sum(c => c.Deuda?.TotalAPagar ?? 0),
                TotalPagosRecientes = await _db.Pagos
                    .Where(p => clientes.Select(c => c.Id).Contains(p.ClienteId) &&
                                p.Fecha >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(p => p.Monto),
                Clientes = clientes.Select(c => c.Nombre).ToList(),
                DeudasPorCliente = clientes.Select(c => c.Deuda?.TotalAPagar ?? 0).ToList()
            };

            return View(vm);
        }

    }
}
