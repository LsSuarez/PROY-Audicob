using Microsoft.AspNetCore.Mvc;
using Audicob.Data;
using Audicob.Models;
using System.Linq;

namespace Audicob.Controllers
{
    public class ClienteGerenteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClienteGerenteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Mostrar todos los clientes aceptados por supervisores
        public IActionResult Index()
        {
            var clientesAceptados = _context.Clientes
                .Where(c => c.Estado == "Aceptado")
                .ToList();

            return View(clientesAceptados);
        }

        // Mostrar detalle del cliente
        public IActionResult Detalle(int id)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
            {
                return NotFound();
            }
            return View(cliente);
        }

        // Guardar decisión del administrador
        [HttpPost]
        public IActionResult TomarDecision(int Id, string Decision, string Motivo)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == Id);
            if (cliente == null)
            {
                return NotFound();
            }

            // Guardar decisión y motivo
            cliente.EstadoAdmin = Decision; // "Aceptado" o "Rechazado"
            cliente.MotivoAdmin = Motivo;
            cliente.FechaDecisionAdmin = DateTime.UtcNow; // ✅ Guardar UTC

            _context.SaveChanges();

            // ✅ Mostrar mensaje en la misma vista
            TempData["Mensaje"] = $"Cliente {Decision.ToLower()} guardado correctamente.";

            // Volver a mostrar la vista detalle con el cliente actualizado
            return RedirectToAction("Detalle", new { id = Id });
        }

        // ✅ Nueva acción: mostrar clientes aceptados por el administrador (gerente)
        public IActionResult ClientesAceptadosAdmin()
        {
            var clientes = _context.Clientes
                .Where(c => c.EstadoAdmin == "Aceptado")
                .OrderByDescending(c => c.FechaDecisionAdmin)
                .ToList();

            return View(clientes);
        }
    }
}
