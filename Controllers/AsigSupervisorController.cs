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
    }
}
