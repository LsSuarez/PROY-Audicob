using Audicob.Data;
using Audicob.Models.ViewModels.Cliente;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Controllers
{
    public class AsigSupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AsigSupervisorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? filtro)
        {
            // Buscar clientes sin asignar
            var query = _context.Clientes
                .Include(c => c.AsignacionAsesor)
                .Where(c => c.AsignacionAsesor == null);

            if (!string.IsNullOrEmpty(filtro))
            {
                query = query.Where(c =>
                    c.Nombre.Contains(filtro) ||
                    c.Documento.Contains(filtro));
            }

            var model = new ClienteDashboardViewModel
            {
                Filtro = filtro,
                ListCliente = query.ToList()
            };

            return View(model);
        }

        //Asignación de Cliente
        public IActionResult Asignar(int id)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null) return NotFound();

            var asesores = _context.AsignacionesAsesores.ToList();

            ViewBag.ClienteId = id;
            ViewBag.ClienteNombre = cliente.Nombre;

            return PartialView("_AsignarAsesorPartial", asesores);
        }


        [HttpPost]
        public IActionResult GuardarAsignacion(int clienteId, int asesorId)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == clienteId);
            var asesor = _context.AsignacionesAsesores.FirstOrDefault(a => a.Id == asesorId);

            if (cliente == null || asesor == null)
                return NotFound();

            cliente.AsignacionAsesor = asesor;
            _context.SaveChanges();

            var lista = _context.AsignacionesAsesores
                .Include(a => a.Clientes)
                .ToList();

            return PartialView("_TablaAsignacionesPartial", lista);
        }

    }
}
