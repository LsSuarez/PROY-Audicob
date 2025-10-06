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
                .Include(a => a.Cliente)
                .Where(a => a.AsesorUserId == user.Id)
                .ToListAsync();

            var vm = new AsesorDashboardViewModel
            {
                TotalClientesAsignados = asignaciones.Count,
                TotalDeudaCartera = asignaciones.Sum(a => a.Cliente.DeudaTotal),
                TotalPagosRecientes = await _db.Pagos
                    .Where(p => asignaciones.Select(a => a.ClienteId).Contains(p.ClienteId) &&
                                p.Fecha >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(p => p.Monto),
                Clientes = asignaciones.Select(a => a.Cliente.Nombre).ToList(),
                DeudasPorCliente = asignaciones.Select(a => a.Cliente.DeudaTotal).ToList()
            };

            return View(vm);
        }
    }
}
