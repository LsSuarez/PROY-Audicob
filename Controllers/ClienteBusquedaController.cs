using Microsoft.AspNetCore.Mvc;
using Audicob.Data;
using Audicob.Models;
using System.Linq;

namespace Audicob.Controllers
{
    public class ClienteBusquedaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClienteBusquedaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================
        // Buscar cliente por documento (Supervisor)
        // ==========================
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                ViewBag.Mensaje = "Por favor, ingrese el documento del cliente.";
                return View();
            }

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Documento == documento);

            if (cliente == null)
            {
                ViewBag.Mensaje = "No se encontró ningún cliente con ese documento.";
                return View();
            }

            return View(cliente);
        }

        // ==========================
        // Aceptar cliente (Supervisor)
        // ==========================
        [HttpPost]
        public IActionResult AceptarCliente(int id)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
                return Json(new { success = false, mensaje = "Cliente no encontrado." });

            cliente.Estado = "Aceptado";
            cliente.UsuarioSupervisor = User.Identity?.Name ?? "Supervisor";
            _context.SaveChanges();

            return Json(new { success = true, mensaje = "Cliente aceptado y enviado al gerente." });
        }

        // ==========================
        // Rechazar cliente (Supervisor)
        // ==========================
        [HttpPost]
        public IActionResult RechazarCliente(int id)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
                return Json(new { success = false, mensaje = "Cliente no encontrado." });

            cliente.Estado = "Rechazado";
            cliente.UsuarioSupervisor = User.Identity?.Name ?? "Supervisor";
            _context.SaveChanges();

            return Json(new { success = true, mensaje = "Cliente rechazado." });
        }

        // ==========================
        // Ver clientes aceptados (Gerente)
        // ==========================
        public IActionResult ClientesAceptados()
        {
            var clientesAceptados = _context.Clientes
                .Where(c => c.Estado == "Aceptado")
                .ToList();

            return View(clientesAceptados);
        }

        // ==========================
        // Ver clientes rechazados por el Gerente/Admin
        // ==========================
        public IActionResult ClientesRechazadosPorAdmin()
        {
            var clientesRechazados = _context.Clientes
                .Where(c => c.EstadoAdmin == "Rechazado")
                .ToList();

            return View("ClientesRechazados", clientesRechazados);
        }
    }
}
