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
    }
}
