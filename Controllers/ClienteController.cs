using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Cliente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class ClienteController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClienteController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Dashboard del cliente
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            // Buscar cliente por UserId
            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Evaluaciones)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cliente == null)
            {
                TempData["Error"] = "No se encontró información del cliente.";
                return RedirectToAction("Index", "Home");
            }

            var vm = new ClienteDashboardViewModel
            {
                Nombre = cliente.Nombre,
                DeudaTotal = cliente.DeudaTotal,
                IngresosMensuales = cliente.IngresosMensuales,
                PagosFechas = cliente.Pagos.Select(p => p.Fecha.ToString("dd/MM/yyyy")).ToList(),
                PagosMontos = cliente.Pagos.Select(p => p.Monto).ToList(),
                PagosRecientes = cliente.Pagos.OrderByDescending(p => p.Fecha).Take(5).Select(p => new PagoResumen
                {
                    Fecha = p.Fecha,
                    Monto = p.Monto,
                    Validado = p.Validado
                }).ToList(),
                Evaluaciones = cliente.Evaluaciones.Select(e => new EvaluacionResumen
                {
                    Fecha = e.Fecha,
                    Estado = e.Estado,
                    Responsable = e.Responsable,
                    Comentario = e.Comentario
                }).ToList()
            };

            return View(vm);
        }
        public async Task<IActionResult> DetalleDeudaTotal()
        {
            var user = await _userManager.GetUserAsync(User);

            // Buscar el cliente autenticado
            var cliente = await _db.Clientes
                .Include(c => c.PagosPendientes) // asegúrate de que esta propiedad existe en tu modelo Cliente
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cliente == null)
            {
                TempData["Error"] = "No se encontró información del cliente.";
                return RedirectToAction("Index", "Home");
            }

            // Obtener sus pagos pendientes
            var deudas = cliente.PagosPendientes?.ToList() ?? new List<PagoPendiente>();

            // Enviar la lista a la vista
            return View(deudas);
        }
        
        // Acción para mostrar el detalle de un pago pendiente
        public async Task<IActionResult> DetallePago(int id)
        {
            // Buscar el pago pendiente por su Id
            var pago = await _db.PagoPendiente.FirstOrDefaultAsync(p => p.Id == id);

            if (pago == null)
            {
                return NotFound();
            }

            // Devolver un partial view con la información del pago
            return PartialView("_DetallePagoPartial", pago);
        }

    }
}